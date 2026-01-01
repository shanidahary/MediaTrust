using MediaTrust.Report.Managers;
using Microsoft.AspNetCore.Mvc;

namespace MediaTrust.Report.Controllers;

[ApiController]
[Route("reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ReportManager _manager;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        ReportManager manager,
        ILogger<ReportsController> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    [HttpGet("media/{mediaId:guid}")]
    public async Task<IActionResult> GetForMedia(
        Guid mediaId,
        CancellationToken ct)
    {
        try
        {
            var report = await _manager.BuildReportAsync(mediaId, ct);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to return report for MediaId {MediaId}",
                mediaId);
            return StatusCode(500, "Report failed");
        }
    }
}