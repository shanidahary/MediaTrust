using MediaTrust.Report.Data;

namespace MediaTrust.Report.Accessors;

public interface IReportRepository
{
    Task<IReadOnlyList<DetectorResult>> GetByMediaIdAsync(
        Guid mediaId,
        CancellationToken ct);
}
