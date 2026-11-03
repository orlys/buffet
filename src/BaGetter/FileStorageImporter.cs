using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.Extensions.Logging;

namespace BaGetter;

/// <summary>
/// One-time import of an upstream BaGetter file-system storage folder into the SQLite storage.
/// Relative paths under the folder are the storage keys, so the existing package metadata keeps resolving.
/// </summary>
public class FileStorageImporter
{
    private static readonly string[] Folders = ["packages", "symbols"];

    private readonly IStorageService _storage;
    private readonly ILogger<FileStorageImporter> _logger;

    public FileStorageImporter(IStorageService storage, ILogger<FileStorageImporter> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <returns>The number of files that conflict with different content already in storage.</returns>
    public async Task<int> ImportAsync(string root, CancellationToken cancellationToken)
    {
        root = Path.GetFullPath(root);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"'{root}' does not exist");

        int imported = 0, existing = 0, conflicts = 0;

        foreach (var folder in Folders)
        {
            var path = Path.Combine(root, folder);
            if (!Directory.Exists(path)) continue;

            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                var key = Path.GetRelativePath(root, file);
                await using var content = File.OpenRead(file);

                // Reads never use the content type, so one value is enough for imported files.
                switch (await _storage.PutAsync(key, content, "binary/octet-stream", cancellationToken))
                {
                    case StoragePutResult.Success:
                        imported++;
                        break;
                    case StoragePutResult.AlreadyExists:
                        existing++;
                        break;
                    default:
                        conflicts++;
                        _logger.LogError("'{Key}' is already stored with different content; it was not imported", key);
                        break;
                }
            }
        }

        _logger.LogInformation(
            "Imported {Imported} files from {Root} ({Existing} were already stored, {Conflicts} conflicts)",
            imported, root, existing, conflicts);

        return conflicts;
    }
}
