using MediaTrust.Detectors.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediaTrust.Detectors.Accessors;

public sealed class DetectorResultRepository : IDetectorResultRepository
{
    private readonly DetectorsDbContext _db;
    private readonly ILogger<DetectorResultRepository> _logger;

    public DetectorResultRepository(
        DetectorsDbContext db,
        ILogger<DetectorResultRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AddAsync(DetectorResult result, CancellationToken ct)
    {
        try
        {
            _db.DetectorResults.Add(result);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save detector result for MediaId {MediaId}", result.MediaId);
            throw;
        }
    }

    public async Task<IReadOnlyList<DetectorResult>> GetByMediaIdAsync(Guid mediaId, CancellationToken ct)
    {
        try
        {
            return await _db.DetectorResults
                .AsNoTracking()
                .Where(x => x.MediaId == mediaId)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch detector results for MediaId {MediaId}", mediaId);
            throw;
        }
    }
}
