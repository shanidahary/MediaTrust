using MediaTrust.Orchestrator.Accessors;
using MediaTrust.Orchestrator.Data;
using MediaTrust.Orchestrator.Models;
using MediaTrust.Orchestrator.Messaging;

namespace MediaTrust.Orchestrator.Managers;

public sealed class AnalysisJobManager
{
    private readonly IAnalysisJobRepository _repo;
    private readonly RabbitMqClient _mq;
    private readonly ILogger<AnalysisJobManager> _logger;

    public AnalysisJobManager(
        IAnalysisJobRepository repo,
        RabbitMqClient mq,
        ILogger<AnalysisJobManager> logger)
    {
        _repo = repo;
        _mq = mq;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AnalysisJob>> GetJobsAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching all analysis jobs");
            return await _repo.GetAllAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch jobs");
            throw;
        }
    }

    public async Task<Guid> CreateJobAsync(
        Guid mediaId,
        string objectKey,
        string contentType,
        long sizeBytes,
        CancellationToken ct)
    {
        try
        {
            var job = new AnalysisJob
            {
                Id = Guid.NewGuid(),
                MediaId = mediaId,
                ObjectKey = objectKey,
                Status = "Pending",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            await _repo.AddAsync(job, ct);

            _mq.Publish("detector.basic", new DetectorRequest
            {
                JobId = job.Id,
                MediaId = mediaId,
                ObjectKey = objectKey,
                ContentType = contentType,
                SizeBytes = sizeBytes
            });

            return job.Id;
        }
        catch
        {
            _logger.LogError("CreateJobAsync failed");
            throw;
        }
    }

    public async Task UpdateJobStatusAsync(
        Guid jobId,
        string status,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Business: set job {JobId} status to {Status}",
            jobId,
            status);

        try
        {
            await _repo.UpdateStatusAsync(jobId, status, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Business failure updating job {JobId}",
                jobId);
            throw;
        }
    }
}