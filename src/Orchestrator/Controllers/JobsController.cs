using Microsoft.AspNetCore.Mvc;
using MediaTrust.Orchestrator.Managers;

namespace MediaTrust.Orchestrator.Controllers;

[ApiController]
[Route("jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly AnalysisJobManager _manager;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        AnalysisJobManager manager,
        ILogger<JobsController> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("GET /jobs requested");

            var jobs = await _manager.GetJobsAsync(ct);

            _logger.LogInformation(
                "Returning {Count} analysis jobs",
                jobs.Count);

            return Ok(jobs);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GET /jobs was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analysis jobs");
            return StatusCode(500, "Failed to retrieve jobs");
        }
    }
}