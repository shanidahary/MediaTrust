using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;

namespace MediaTrust.Detectors.Storage;

public sealed class MinioStorage
{
    private readonly IMinioClient _client;
    private readonly MinioOptions _options;

    public MinioStorage(
        IMinioClient client,
        IOptions<MinioOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<Stream> GetAsync(
        string objectKey,
        CancellationToken ct)
    {
        var ms = new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(_options.Bucket)
            .WithObject(objectKey)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(ms);
            });

        await _client.GetObjectAsync(args, ct);

        ms.Position = 0;
        return ms;
    }
}
