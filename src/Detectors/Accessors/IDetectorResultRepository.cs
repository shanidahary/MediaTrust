using MediaTrust.Detectors.Data;

namespace MediaTrust.Detectors.Accessors;

public interface IDetectorResultRepository
{
    Task AddAsync(DetectorResult result, CancellationToken ct);
    Task<IReadOnlyList<DetectorResult>> GetByMediaIdAsync(Guid mediaId, CancellationToken ct);
}
