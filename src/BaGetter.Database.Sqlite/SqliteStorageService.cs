using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace BaGetter.Database.Sqlite;

/// <summary>
/// Stores package and symbol files as blobs in the same SQLite database as the metadata,
/// so the whole feed is a single file.
/// </summary>
/// <remarks>The table is created by the <c>AddStorageBlobs</c> migration.</remarks>
public class SqliteStorageService : IStorageService
{
    internal const string TableName = "StorageBlobs";

    // SQLITE_MAX_LENGTH in the bundled SQLite build; a larger zeroblob() fails with SQLITE_TOOBIG.
    private const long MaxBlobLength = 1_000_000_000;

    private readonly string _connectionString;

    public SqliteStorageService(IOptionsSnapshot<DatabaseOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _connectionString = options.Value.ConnectionString;
    }

    public async Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        var connection = await OpenAsync(cancellationToken);
        try
        {
            var rowId = await FindRowIdAsync(connection, NormalizePath(path), cancellationToken);
            if (rowId == null)
            {
                await connection.DisposeAsync();
                return null;
            }

            var blob = new SqliteBlob(connection, TableName, "Content", rowId.Value, readOnly: true);
            return new ConnectionOwningStream(blob, connection);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    // There is no URL for a blob inside the database; content is only served through GetAsync.
    public Task<Uri?> GetDownloadUriAsync(string path, CancellationToken cancellationToken = default)
        => Task.FromResult<Uri?>(null);

    public async Task<StoragePutResult> PutAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrEmpty(contentType)) throw new ArgumentException("Content type is required", nameof(contentType));

        path = NormalizePath(path);

        // A blob is written into a zeroblob() of its final size, so the length must be known up front.
        var buffer = content.CanSeek ? null : await BufferToTempFileAsync(content, cancellationToken);
        var source = buffer ?? content;

        try
        {
            var start = source.Position;
            var length = source.Length - start;
            if (length > MaxBlobLength)
            {
                throw new InvalidOperationException(
                    $"'{path}' is {length:N0} bytes; SQLite storage holds at most {MaxBlobLength:N0} bytes per file.");
            }

            await using var connection = await OpenAsync(cancellationToken);

            // BEGIN IMMEDIATE takes the write lock now, so two pushes of the same path cannot interleave.
            await using var transaction = connection.BeginTransaction(deferred: false);

            var existingRowId = await FindRowIdAsync(connection, path, cancellationToken, transaction);
            if (existingRowId != null)
            {
                using var stored = new SqliteBlob(connection, TableName, "Content", existingRowId.Value, readOnly: true);
                source.Position = start;

                return source.Matches(stored)
                    ? StoragePutResult.AlreadyExists
                    : StoragePutResult.Conflict;
            }

            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                $"INSERT INTO {TableName} (Path, ContentType, Content) VALUES ($path, $contentType, zeroblob($length)); " +
                "SELECT last_insert_rowid();";
            insert.Parameters.AddWithValue("$path", path);
            insert.Parameters.AddWithValue("$contentType", contentType);
            insert.Parameters.AddWithValue("$length", length);
            var rowId = (long)(await insert.ExecuteScalarAsync(cancellationToken))!;

            using (var blob = new SqliteBlob(connection, TableName, "Content", rowId, readOnly: false))
            {
                await source.CopyToAsync(blob, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return StoragePutResult.Success;
        }
        finally
        {
            if (buffer != null) await buffer.DisposeAsync();
        }
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var delete = connection.CreateCommand();
        delete.CommandText = $"DELETE FROM {TableName} WHERE Path = $path;";
        delete.Parameters.AddWithValue("$path", NormalizePath(path));
        await delete.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<long?> FindRowIdAsync(
        SqliteConnection connection,
        string path,
        CancellationToken cancellationToken,
        SqliteTransaction? transaction = null)
    {
        await using var find = connection.CreateCommand();
        find.Transaction = transaction;
        find.CommandText = $"SELECT rowid FROM {TableName} WHERE Path = $path;";
        find.Parameters.AddWithValue("$path", path);

        return await find.ExecuteScalarAsync(cancellationToken) as long?;
    }

    // Callers build paths with Path.Combine; store '/' so a database moved between Windows and Linux still resolves.
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path is required", nameof(path));

        return path.Replace('\\', '/');
    }

    private static async Task<Stream> BufferToTempFileAsync(Stream content, CancellationToken cancellationToken)
    {
        var file = new FileStream(
            Path.GetTempFileName(),
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        await content.CopyToAsync(file, cancellationToken);
        file.Position = 0;
        return file;
    }

    /// <summary>
    /// A read-only blob stream that closes its connection when disposed.
    /// </summary>
    private sealed class ConnectionOwningStream(SqliteBlob blob, SqliteConnection connection) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => blob.Length;

        public override long Position
        {
            get => blob.Position;
            set => blob.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count) => blob.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => blob.Read(buffer);
        public override long Seek(long offset, SeekOrigin origin) => blob.Seek(offset, origin);
        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                blob.Dispose();
                connection.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
