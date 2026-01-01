using System.Net.Http.Json;
using MediaTrust.Ingest.Accessors;
using MediaTrust.Ingest.Data;
using MediaTrust.Ingest.Models;

namespace MediaTrust.Ingest.Managers;

public sealed class MediaIngestManager
{
    private readonly IMediaRepository _repo;
    private readonly IStorageAccessor _storage;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<MediaIngestManager> _logger;

    public MediaIngestManager(
        IMediaRepository repo,
        IStorageAccessor storage,
        IHttpClientFactory http,
        ILogger<MediaIngestManager> logger)
    {
        _repo = repo;
        _storage = storage;
        _http = http;
        _logger = logger;
    }

    public async Task<UploadMediaResult> HandleUploadAsync(HttpRequest req, CancellationToken ct)
    {
        try
        {
            if (!req.HasFormContentType)
                throw new InvalidOperationException("Expected multipart/form-data");

            var file = (await req.ReadFormAsync(ct)).Files.GetFile("file");
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Missing file");

            var name = Path.GetFileName(file.FileName);
            if (await _repo.ExistsByFileNameAsync(name, ct))
                throw new InvalidOperationException("Duplicate file");

            var mediaId = Guid.NewGuid();
            var objectKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{name}";

            await using var s = file.OpenReadStream();
            await _storage.UploadAsync(objectKey, s, file.Length, file.ContentType!, ct);

            var entity = new MediaItem
            {
                Id = mediaId,
                FileName = name,
                ContentType = file.ContentType!,
                SizeBytes = file.Length,
                ObjectKey = objectKey,
                UploadedAtUtc = DateTimeOffset.UtcNow
            };

            await _repo.AddAsync(entity, ct);

            var client = _http.CreateClient("orchestrator");
            await client.PostAsJsonAsync("jobs", new CreateJobRequest
            {
                MediaId = mediaId,
                ObjectKey = objectKey,
                ContentType = entity.ContentType,
                SizeBytes = entity.SizeBytes
            }, ct);

            return new UploadMediaResult
            {
                MediaId = mediaId,
                ObjectKey = objectKey,
                FileName = name,
                ContentType = entity.ContentType,
                SizeBytes = entity.SizeBytes,
                UploadedAtUtc = entity.UploadedAtUtc
            };
        }
        catch
        {
            _logger.LogError("HandleUploadAsync failed");
            throw;
        }
    }
}