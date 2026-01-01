using Microsoft.AspNetCore.Mvc;
using MediaTrust.Orchestrator.Managers;
using MediaTrust.Orchestrator.Models;

namespace MediaTrust.Orchestrator.Controllers;

[ApiController]
[Route("jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly AnalysisJobManager _manager;
    private readonly ILogger<JobsController> _logger;

    public JobsController(AnalysisJobManager manager, ILogger<JobsController> logger)
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

            var response = jobs.Select(j => new
            {
                jobId = j.Id,
                mediaId = j.MediaId,
                objectKey = j.ObjectKey,
                status = j.Status,
                createdAtUtc = j.CreatedAtUtc,
                updatedAtUtc = j.UpdatedAtUtc
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get jobs");
            return StatusCode(500, "Failed to retrieve jobs");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateJobHttpRequest req, CancellationToken ct)
    {
        var id = await _manager.CreateJobAsync(
            req.MediaId, req.ObjectKey, req.ContentType, req.SizeBytes, ct);

        return Ok(new { jobId = id });
    }

    [HttpPatch("{jobId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid jobId,
        [FromBody] UpdateJobStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Updating job {JobId} to {Status}",
                jobId,
                request.Status);

            await _manager.UpdateJobStatusAsync(jobId, request.Status, ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Job not found {JobId}", jobId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job status");
            return StatusCode(500, "Internal error");
        }
    }
}