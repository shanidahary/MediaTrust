using MediaTrust.Detectors.Accessors;
using MediaTrust.Detectors.Data;
using MediaTrust.Detectors.Models;

namespace MediaTrust.Detectors.Managers;

public sealed class DetectorManager
{
    private readonly IDetectorResultRepository _repo;
    private readonly IHttpClientFactory  _http;
    private readonly ILogger<DetectorManager> _logger;

    public DetectorManager(
        IDetectorResultRepository repo,
        IHttpClientFactory http,
        ILogger<DetectorManager> logger)
    {
        _repo = repo;
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

            // Detector logic
            var score = req.SizeBytes == 0 ? 1.0 : 0.2;

            await _repo.AddAsync(new DetectorResult
            {
                Id = Guid.NewGuid(),
                MediaId = req.MediaId,
                DetectorName = "BasicDetector",
                Score = score,
                Details = $"Size={req.SizeBytes}",
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
}