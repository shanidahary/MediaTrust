using MediaTrust.Detectors.Managers;
using MediaTrust.Detectors.Messaging;

namespace MediaTrust.Detectors.Workers;

public sealed class DetectorWorker : BackgroundService
{
    private readonly RabbitMqClient _mq;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DetectorWorker> _logger;

    public DetectorWorker(
        RabbitMqClient mq,
        IServiceScopeFactory scopeFactory,
        ILogger<DetectorWorker> logger)
    {
        _mq = mq;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Detector worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var message = _mq.Pull();

            if (message == null)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    stoppingToken);

                continue;
            }

            try
            {
                _logger.LogInformation(
                    "Processing detector message. MediaId={MediaId}",
                    message.Request.MediaId);

                using var scope =
                    _scopeFactory.CreateScope();

                var manager =
                    scope.ServiceProvider
                        .GetRequiredService<DetectorManager>();

                await manager.RunOnceAsync(
                    message.Request,
                    stoppingToken);

                _mq.Ack(message.DeliveryTag);

                _logger.LogInformation(
                    "Detector message completed. MediaId={MediaId}",
                    message.Request.MediaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Automatic detector processing failed");

                _mq.Nack(
                    message.DeliveryTag,
                    requeue: true);

                await Task.Delay(
                    TimeSpan.FromSeconds(2),
                    stoppingToken);
            }
        }
    }
}