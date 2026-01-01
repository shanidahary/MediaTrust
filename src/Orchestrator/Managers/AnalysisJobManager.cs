using MediaTrust.Orchestrator.Accessors;
using MediaTrust.Orchestrator.Data;
using MediaTrust.Orchestrator.Models;

namespace MediaTrust.Orchestrator.Managers;

public sealed class AnalysisJobManager
{
    private readonly IAnalysisJobRepository _repository;
    private readonly ILogger<AnalysisJobManager> _logger;

    public AnalysisJobManager(
        IAnalysisJobRepository repository,
        ILogger<AnalysisJobManager> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task CreateJobAsync(
        Guid mediaId,
        string objectKey,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Creating analysis job. MediaId={MediaId}",
            mediaId);

        var job = new AnalysisJob
        {
            Id = Guid.NewGuid(),
            MediaId = mediaId,
            ObjectKey = objectKey,
            Status = "Pending",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        try
        {
            await _repository.AddAsync(job, ct);

            _logger.LogInformation(
                "Analysis job persisted. JobId={JobId}",
                job.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "CreateJobAsync cancelled. MediaId={MediaId}",
                mediaId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist analysis job. MediaId={MediaId}",
                mediaId);
            throw;
        }
    }

    public async Task<IReadOnlyList<AnalysisJobDto>> GetJobsAsync(CancellationToken ct)
    {
        try
        {
            var jobs = await _repository.GetAllAsync(ct);

            return jobs.Select(j => new AnalysisJobDto
            {
                JobId = j.Id,
                MediaId = j.MediaId,
                ObjectKey = j.ObjectKey,
                Status = j.Status,
                CreatedAtUtc = j.CreatedAtUtc
            }).ToList();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetJobsAsync cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load analysis jobs");
            throw;
        }
    }
}