using MediaTrust.Orchestrator.Data;

namespace MediaTrust.Orchestrator.Accessors;

public interface IAnalysisJobRepository
{
    Task AddAsync(AnalysisJob job, CancellationToken ct);
    Task<IReadOnlyList<AnalysisJob>> GetAllAsync(CancellationToken ct);

    Task<AnalysisJob?> GetLatestByMediaIdAsync(Guid mediaId, CancellationToken ct);
    Task UpdateStatusAsync(Guid jobId, string status, CancellationToken ct);
}
