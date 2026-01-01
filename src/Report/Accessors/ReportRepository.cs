using MediaTrust.Report.Data;
using Microsoft.EntityFrameworkCore;

namespace MediaTrust.Report.Accessors;

public sealed class ReportRepository : IReportRepository
{
    private readonly ReportDbContext _db;
    private readonly ILogger<ReportRepository> _logger;

    public ReportRepository(
        ReportDbContext db,
        ILogger<ReportRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DetectorResult>> GetByMediaIdAsync(
        Guid mediaId,
        CancellationToken ct)
    {
        try
        {
            return await _db.DetectorResults
                .AsNoTracking()
                .Where(x => x.MediaId == mediaId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load detector results for MediaId {MediaId}",
                mediaId);
            throw;
        }
    }
}