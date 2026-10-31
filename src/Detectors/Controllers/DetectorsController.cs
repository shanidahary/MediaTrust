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

            var msg = _mq.Pull();

            if (msg == null)
            {
                return Ok("Queue empty");
            }

            try
            {
                await _manager.RunOnceAsync(
                msg.Request,
                ct);

            _mq.Ack(msg.DeliveryTag);

            return Ok("Detector executed");
            }
            catch
            {
                _mq.Nack(
                msg.DeliveryTag,
                requeue: true);

            throw;
            }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detector execution failed");

                return StatusCode(
                    500,
                    "Detector failed");
            }
        }

[HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("pong");
    }

    [HttpGet("results/{mediaId:guid}")]
    public async Task<IActionResult> GetResults(
        Guid mediaId,
        CancellationToken ct)
    {
        try
        {
            var results = await _manager.GetResultsAsync(
                mediaId,
                ct);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get detector results. MediaId={MediaId}",
                mediaId);

            return StatusCode(
                500,
                "Failed to get detector results");
        }
    }
    [HttpGet("results/job/{jobId:guid}")]
    public async Task<IActionResult> GetResultsByJobId(
        Guid jobId,
        CancellationToken ct)
    {
        try
        {
            var results = await _manager.GetResultsByJobIdAsync(
                jobId,
                ct);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get detector results. JobId={JobId}",
                jobId);

            return StatusCode(
                500,
                "Failed to get detector results");
        }
    }
}