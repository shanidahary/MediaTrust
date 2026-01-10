using MediaTrust.Detectors.Accessors;
using MediaTrust.Detectors.Data;
using MediaTrust.Detectors.Models;
using MediaTrust.Detectors.Storage;
using MediaTrust.Detectors.Analysis;

namespace MediaTrust.Detectors.Managers;

public sealed class DetectorManager
{
    private static readonly Dictionary<string, byte[]> MagicHeaders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            ["image/png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
            ["image/gif"] = new byte[] { 0x47, 0x49, 0x46, 0x38 },
            ["application/pdf"] = new byte[] { 0x25, 0x50, 0x44, 0x46 },
            ["video/mp4"] = new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 }
        };

    private readonly IDetectorResultRepository _repo;
    private readonly MinioStorage _storage;
    private readonly IHttpClientFactory  _http;
    private readonly ILogger<DetectorManager> _logger;

    public DetectorManager(
        IDetectorResultRepository repo,
        MinioStorage storage,
        IHttpClientFactory http,
        ILogger<DetectorManager> logger)
    {
        _repo = repo;
        _storage = storage;
        _http = http;
        _logger = logger;
    }

    public async Task RunOnceAsync(
        DetectorRequest req,
        CancellationToken ct)
    {
         var client = _http.CreateClient("gateway");

        _logger.LogInformation(
            "Detector started. JobId={JobId}, MediaId={MediaId}",
            req.JobId,
            req.MediaId);

        try
        {
            // Update job → Processing
            await client.PatchAsJsonAsync(
                $"jobs/{req.JobId}/status",
                new { Status = "Processing" },
                ct);

            // Download file from MinIO
            await using var stream = await _storage.GetAsync(
                req.ObjectKey,
                ct);

            // Read header
            var header = await ReadHeaderAsync(stream, 8, ct);

            // Magic bytes validation
            var headerMatch =
                MagicHeaders.TryGetValue(req.ContentType, out var expected) &&
                header.Length >= expected.Length &&
                header.AsSpan(0, expected.Length).SequenceEqual(expected);

            // Entropy check (first 4KB)
            stream.Position = 0;
            var buffer = new byte[(int)Math.Min(4096, req.SizeBytes)];
            await stream.ReadExactlyAsync(buffer, ct);
            var entropy = DetectorUtils.CalculateEntropy(buffer);

            // Score logic
            var score = 0.0;
            if (!headerMatch) score += 0.6;
            if (entropy > 7.2) score += 0.4;

            score = Math.Min(score, 1.0);

            await _repo.AddAsync(new DetectorResult
            {
                Id = Guid.NewGuid(),
                MediaId = req.MediaId,
                DetectorName = "ContentIntegrityDetector",
                Score = score,
                Details = $"HeaderMatch={headerMatch}, Entropy={entropy:F2}",
                CreatedAtUtc = DateTimeOffset.UtcNow
            }, ct);

            // Update job → Completed
            await client.PatchAsJsonAsync(
                $"jobs/{req.JobId}/status",
                new { Status = "Completed" },
                ct);

            _logger.LogInformation(
                "Detector completed successfully. MediaId={MediaId}",
                req.MediaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Detector failed. MediaId={MediaId}",
                req.MediaId);

            // Update job → Failed
            await client.PatchAsJsonAsync(
                $"jobs/{req.JobId}/status",
                new { Status = "Failed" },
                ct);

            throw;
        }
    }

    private static async Task<byte[]> ReadHeaderAsync(
        Stream stream,
        int length,
        CancellationToken ct)
    {
        var buffer = new byte[length];
        var read = 0;

        while (read < length)
        {
            var n = await stream.ReadAsync(
                buffer.AsMemory(read, length - read),
                ct);

            if (n == 0)
            {
                break;
            }

            read += n;
        }

        if (read == buffer.Length)
        {
            return buffer;
        }

        Array.Resize(ref buffer, read);
        return buffer;
    }
}
