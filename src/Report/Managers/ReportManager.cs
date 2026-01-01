using MediaTrust.Report.Accessors;
using MediaTrust.Report.Models;

namespace MediaTrust.Report.Managers;

public sealed class ReportManager
{
    private readonly IReportRepository _repo;
    private readonly ILogger<ReportManager> _logger;

    public ReportManager(
        IReportRepository repo,
        ILogger<ReportManager> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<MediaReportDto> BuildReportAsync(
        Guid mediaId,
        CancellationToken ct)
    {
        try
        {
            var results = await _repo.GetByMediaIdAsync(mediaId, ct);

            var riskScore = results.Any()
                ? results.Max(x => x.Score)
                : 0;

            var status = riskScore switch
            {
                < 0.3 => "OK",
                < 0.7 => "Warning",
                _ => "Critical"
            };

            return new MediaReportDto
            {
                MediaId = mediaId,
                RiskScore = riskScore,
                OverallStatus = status,
                Results = results.Select(x => new DetectorResultDto
                {
                    DetectorName = x.DetectorName,
                    Score = x.Score,
                    Details = x.Details
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to build report for MediaId {MediaId}",
                mediaId);
            throw;
        }
    }
}