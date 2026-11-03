using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core.Tests.Support;
public class NullStorageService : IStorageService
{
    private HashSet<string> _files = new();

    public NullStorageService()
    {
    }

    // Like the real storage, deleting a path that was never stored (e.g. a missing readme) is a no-op.
    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        _files.Remove(path);
        return Task.CompletedTask;
    }

    public Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Uri> GetDownloadUriAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<StoragePutResult> PutAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        _files.Add(path);
        return Task.FromResult(StoragePutResult.Success);
    }
}
