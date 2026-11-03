using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BaGetter.Core;
using BaGetter.Database.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BaGetter.Tests;

public class SqliteStorageServiceTests : IDisposable
{
    private readonly string _databasePath;
    private readonly SqliteStorageService _target;

    public SqliteStorageServiceTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"bagetter-storage-{Guid.NewGuid():N}.db");

        // A short busy timeout turns lock waits into fast failures.
        var connectionString = $"Data Source={_databasePath};Default Timeout=2";

        // Start from a pre-existing file in rollback-journal mode, like a database created by upstream BaGetter.
        // (EF only switches to WAL when it creates the file itself.)
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
        }

        // Create the StorageBlobs table the same way the app does at startup.
        var efOptions = new DbContextOptionsBuilder<SqliteContext>().UseSqlite(connectionString).Options;
        using (var context = new SqliteContext(efOptions, new Snapshot<BaGetterOptions>(new BaGetterOptions())))
        {
            context.RunMigrationsAsync(default).GetAwaiter().GetResult();
        }

        _target = new SqliteStorageService(new Snapshot<DatabaseOptions>(new DatabaseOptions { ConnectionString = connectionString }));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }

    [Fact]
    public async Task PutThenGetReturnsTheContent()
    {
        Assert.Equal(StoragePutResult.Success, await _target.PutAsync("packages/a/1.0.0/a.nupkg", Content("hello"), "binary/octet-stream"));

        Assert.Equal("hello", await ReadAsync("packages/a/1.0.0/a.nupkg"));
    }

    [Fact]
    public async Task BackslashAndSlashPathsAreTheSameFile()
    {
        await _target.PutAsync(@"packages\a\1.0.0\a.nupkg", Content("hello"), "binary/octet-stream");

        Assert.Equal("hello", await ReadAsync("packages/a/1.0.0/a.nupkg"));
    }

    [Fact]
    public async Task AcceptsNonSeekableContent()
    {
        var content = new NonSeekableStream(Content("streamed"));

        Assert.Equal(StoragePutResult.Success, await _target.PutAsync("x", content, "text/plain"));
        Assert.Equal("streamed", await ReadAsync("x"));
    }

    [Fact]
    public async Task PuttingTheSameContentAgainReportsAlreadyExists()
    {
        await _target.PutAsync("x", Content("same"), "text/plain");

        Assert.Equal(StoragePutResult.AlreadyExists, await _target.PutAsync("x", Content("same"), "text/plain"));
    }

    [Fact]
    public async Task PuttingDifferentContentReportsConflictAndKeepsTheOriginal()
    {
        await _target.PutAsync("x", Content("first"), "text/plain");

        Assert.Equal(StoragePutResult.Conflict, await _target.PutAsync("x", Content("second"), "text/plain"));
        Assert.Equal("first", await ReadAsync("x"));
    }

    [Fact]
    public async Task AnOpenDownloadDoesNotBlockWrites()
    {
        await _target.PutAsync("held", Content("being downloaded"), "text/plain");

        // A download keeps its blob stream open while the response is sent; pushes and download counters must still commit.
        using var download = await _target.GetAsync("held");

        Assert.Equal(StoragePutResult.Success, await _target.PutAsync("other", Content("pushed meanwhile"), "text/plain"));
    }

    [Fact]
    public async Task GetReturnsNullForMissingContent()
    {
        Assert.Null(await _target.GetAsync("missing"));
    }

    [Fact]
    public async Task DeleteRemovesContentAndIgnoresMissingPaths()
    {
        await _target.PutAsync("x", Content("bye"), "text/plain");

        await _target.DeleteAsync("x");
        await _target.DeleteAsync("never-existed");

        Assert.Null(await _target.GetAsync("x"));
    }

    [Fact]
    public async Task ImportsAnUpstreamStorageFolderUnderTheSameKeys()
    {
        var root = Path.Combine(Path.GetTempPath(), $"bagetter-import-{Guid.NewGuid():N}");
        try
        {
            WriteFile(root, "packages/a/1.0.0/a.1.0.0.nupkg", "package");
            WriteFile(root, "symbols/a.pdb/abc123ffffffff/a.pdb", "symbols");
            WriteFile(root, "db/bagetter.db", "not storage");

            var importer = new FileStorageImporter(_target, NullLogger<FileStorageImporter>.Instance);

            Assert.Equal(0, await importer.ImportAsync(root, default));
            Assert.Equal(0, await importer.ImportAsync(root, default)); // re-running is harmless

            Assert.Equal("package", await ReadAsync("packages/a/1.0.0/a.1.0.0.nupkg"));
            Assert.Equal("symbols", await ReadAsync("symbols/a.pdb/abc123ffffffff/a.pdb"));
            Assert.Null(await _target.GetAsync("db/bagetter.db"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteFile(string root, string relativePath, string text)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, text);
    }

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));

    private async Task<string> ReadAsync(string path)
    {
        using var stream = await _target.GetAsync(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private sealed class Snapshot<T>(T value) : IOptionsSnapshot<T> where T : class
    {
        public T Value => value;
        public T Get(string name) => value;
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
    }
}
