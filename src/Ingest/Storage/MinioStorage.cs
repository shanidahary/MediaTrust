using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;

namespace MediaTrust.Ingest.Storage;

public sealed class MinioStorage
{
    private readonly IMinioClient _minio;
    private readonly MinioOptions _opt;

    public MinioStorage(
        IMinioClient minio,
        IOptions<MinioOptions> options)
    {
        _minio = minio;
        _opt = options.Value;
    }

    public async Task EnsureBucketAsync(CancellationToken ct)
    {
        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_opt.Bucket),
            ct);

        if (!exists)
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_opt.Bucket),
                ct);
        }
    }

    public async Task PutObjectAsync(
        string objectKey,
        Stream data,
        long size,
        string contentType,
        CancellationToken ct)
    {
        await EnsureBucketAsync(ct);

        await _minio.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_opt.Bucket)
                .WithObject(objectKey)
                .WithStreamData(data)
                .WithObjectSize(size)
                .WithContentType(contentType),
            ct);
    }
}