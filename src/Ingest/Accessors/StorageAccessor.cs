using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Logging;
using MediaTrust.Ingest.Storage;

namespace MediaTrust.Ingest.Accessors;

public sealed class StorageAccessor : IStorageAccessor
{
    private readonly IMinioClient _minio;
    private readonly MinioOptions _opt;
    private readonly ILogger<StorageAccessor> _logger;

    public StorageAccessor(
        IMinioClient minio,
        MinioOptions opt,
        ILogger<StorageAccessor> logger)
    {
        _minio = minio;
        _opt = opt;
        _logger = logger;
    }

    public async Task UploadAsync(
        string objectKey,
        Stream data,
        long size,
        string contentType,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Uploading object {ObjectKey} to bucket {Bucket}",
                objectKey,
                _opt.Bucket);

            var exists = await _minio.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_opt.Bucket),
                ct);

            if (!exists)
            {
                _logger.LogInformation(
                    "Bucket {Bucket} does not exist. Creating...",
                    _opt.Bucket);

                await _minio.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_opt.Bucket),
                    ct);
            }

            await _minio.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_opt.Bucket)
                    .WithObject(objectKey)
                    .WithStreamData(data)
                    .WithObjectSize(size)
                    .WithContentType(contentType),
                ct);

            _logger.LogInformation(
                "Object {ObjectKey} uploaded successfully",
                objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to upload object {ObjectKey} to bucket {Bucket}",
                objectKey,
                _opt.Bucket);

            throw;
        }
    }
}