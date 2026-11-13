using MediaTrust.Report.Models;

namespace MediaTrust.Report.Accessors;

public interface IDetectorsAccessor
{
    Task<IReadOnlyList<DetectorResultDto>> GetByJobIdAsync(
        Guid jobId,
        CancellationToken ct);
}