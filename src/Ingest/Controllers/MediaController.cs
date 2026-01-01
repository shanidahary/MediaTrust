using Microsoft.AspNetCore.Mvc;
using MediaTrust.Ingest.Managers;

namespace MediaTrust.Ingest.Controllers;

[ApiController]
[Route("media")]
public sealed class MediaController : ControllerBase
{
    private readonly MediaIngestManager _manager;
    private readonly ILogger<MediaController> _logger;

    public MediaController(MediaIngestManager manager, ILogger<MediaController> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(CancellationToken ct)
    {
        try
        {
            return Ok(await _manager.HandleUploadAsync(Request, ct));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid upload");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed");
            return StatusCode(500, "Upload failed");
        }
    }
}