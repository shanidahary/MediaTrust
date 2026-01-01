using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MediaTrust.Ingest.Storage;

namespace MediaTrust.Ingest.Accessors;

public sealed class StorageAccessor : IStorageAccessor
{
    private readonly MinioStorage _storage;

    public StorageAccessor(MinioStorage storage)
    {
        _storage = storage;
    }

    public Task UploadAsync(
        string objectKey,
        Stream data,
        long size,
        string contentType,
        CancellationToken ct)
    {
        return _storage.PutObjectAsync(
            objectKey,
            data,
            size,
            contentType,
            ct);
    }
}
