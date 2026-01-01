using MediaTrust.Orchestrator.Data;
using Microsoft.EntityFrameworkCore;

namespace MediaTrust.Orchestrator.Accessors;

public sealed class AnalysisJobRepository : IAnalysisJobRepository
{
    private readonly OrchestratorDbContext _db;
    private readonly ILogger<AnalysisJobRepository> _logger;

    public AnalysisJobRepository(
        OrchestratorDbContext db,
        ILogger<AnalysisJobRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AddAsync(AnalysisJob job, CancellationToken ct)
    {
        try
        {
            _db.AnalysisJobs.Add(job);
            await _db.SaveChangesAsync(ct);

            _logger.LogDebug(
                "AnalysisJob saved. JobId={JobId}",
                job.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "AddAsync cancelled. JobId={JobId}",
                job.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save AnalysisJob. JobId={JobId}",
                job.Id);
            throw;
        }
    }

    public async Task<IReadOnlyList<AnalysisJob>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            return await _db.AnalysisJobs
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetAllAsync cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query AnalysisJobs");
            throw;
        }
    }

    public async Task UpdateStatusAsync(
        Guid jobId,
        string status,
        CancellationToken ct)
    {
        try
        {
            var job = await _db.AnalysisJobs
                .FirstOrDefaultAsync(x => x.Id == jobId, ct);

            if (job == null)
                throw new InvalidOperationException("Job not found");

            job.Status = status;
            job.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DB failure updating job {JobId}",
                jobId);
            throw;
        }
    }
}