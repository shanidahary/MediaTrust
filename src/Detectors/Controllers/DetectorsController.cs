using Microsoft.AspNetCore.Mvc;
using MediaTrust.Detectors.Messaging;
using MediaTrust.Detectors.Managers;

namespace MediaTrust.Detectors.Controllers;

[ApiController]
[Route("detectors")]
public sealed class DetectorsController : ControllerBase
{
    private readonly RabbitMqClient _mq;
    private readonly DetectorManager _manager;
    private readonly ILogger<DetectorsController> _logger;

    public DetectorsController(
        RabbitMqClient mq,
        DetectorManager manager,
        ILogger<DetectorsController> logger)
    {
        _mq = mq;
        _manager = manager;
        _logger = logger;
    }

    [HttpPost("run-once")]
    public async Task<IActionResult> RunOnce(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Detector run-once triggered");

            // Pull ONE message
            var msg = _mq.Pull();
            if (msg == null)
                return Ok("Queue empty");

            // Call manager (THIS is the call)
            await _manager.RunOnceAsync(msg, ct);

            return Ok("Detector executed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detector execution failed");
            return StatusCode(500, "Detector failed");
        }
    }
}
