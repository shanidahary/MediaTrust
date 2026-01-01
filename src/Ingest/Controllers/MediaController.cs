using Microsoft.AspNetCore.Mvc;
using MediaTrust.Ingest.Managers;
using Microsoft.Extensions.Logging;

namespace MediaTrust.Ingest.Controllers;

[ApiController]
[Route("media")]
public sealed class MediaController : ControllerBase
{
    private readonly MediaIngestManager _manager;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        MediaIngestManager manager,
        ILogger<MediaController> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(CancellationToken ct)
    {
        try
        {
            var result = await _manager.HandleUploadAsync(Request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid upload request");
            return BadRequest(ex.Message);
        }
    }
}