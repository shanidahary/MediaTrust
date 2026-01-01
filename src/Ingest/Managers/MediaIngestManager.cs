using MassTransit;
using MediaTrust.Contracts.Events;
using MediaTrust.Ingest.Accessors;
using MediaTrust.Ingest.Data;
using MediaTrust.Ingest.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MediaTrust.Ingest.Managers;

public sealed class MediaIngestManager
{
    private readonly IMediaRepository _repository;
    private readonly IStorageAccessor _storage;
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<MediaIngestManager> _logger;

    public MediaIngestManager(
        IMediaRepository repository,
        IStorageAccessor storage,
        IPublishEndpoint bus,
        ILogger<MediaIngestManager> logger)
    {
        _repository = repository;
        _storage = storage;
        _bus = bus;
        _logger = logger;
    }

    public async Task<UploadMediaResult> HandleUploadAsync(
    HttpRequest request,
    CancellationToken ct)
    {
        if (!request.HasFormContentType)
            throw new InvalidOperationException("Expected multipart/form-data.");

        var form = await request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file");

        if (file is null || file.Length == 0)
            throw new InvalidOperationException("Missing file.");

        var safeName = Path.GetFileName(file.FileName);

        // CHECK FIRST
        if (await _repository.ExistsByFileNameAsync(safeName, ct))
        {
            _logger.LogWarning(
                "Duplicate file upload blocked. FileName={FileName}",
                safeName);

            throw new InvalidOperationException(
                $"File '{safeName}' already exists.");
        }

        // ONLY NOW generate ID + ObjectKey
        var mediaId = Guid.NewGuid();
        var objectKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{safeName}";

        _logger.LogInformation(
            "Uploading new media. MediaId={MediaId}, FileName={FileName}",
            mediaId,
            safeName);

        await using var stream = file.OpenReadStream();
        await _storage.UploadAsync(
            objectKey,
            stream,
            file.Length,
            file.ContentType ?? "application/octet-stream",
            ct);

        var item = new MediaItem
        {
            Id = mediaId,
            FileName = safeName,
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
            ObjectKey = objectKey,
            UploadedAtUtc = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(item, ct);

        await _bus.Publish(new MediaUploaded(
            mediaId,
            objectKey,
            item.FileName,
            item.ContentType,
            item.SizeBytes,
            item.UploadedAtUtc
        ), ct);

        return new UploadMediaResult
        {
            MediaId = mediaId,
            ObjectKey = objectKey,
            FileName = item.FileName,
            ContentType = item.ContentType,
            SizeBytes = item.SizeBytes,
            UploadedAtUtc = item.UploadedAtUtc
        };
    }
}