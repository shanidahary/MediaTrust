using MassTransit;
using MediaTrust.Contracts.Events;
using MediaTrust.Orchestrator.Managers;

namespace MediaTrust.Orchestrator.Consumers;

public sealed class MediaUploadedConsumer : IConsumer<MediaUploaded>
{
    private readonly AnalysisJobManager _manager;
    private readonly ILogger<MediaUploadedConsumer> _logger;

    public MediaUploadedConsumer(
        AnalysisJobManager manager,
        ILogger<MediaUploadedConsumer> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MediaUploaded> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Received MediaUploaded event. MediaId={MediaId}, ObjectKey={ObjectKey}",
            msg.MediaId,
            msg.ObjectKey);

        try
        {
            await _manager.CreateJobAsync(
                msg.MediaId,
                msg.ObjectKey,
                context.CancellationToken);

            _logger.LogInformation(
                "Analysis job created for MediaId={MediaId}",
                msg.MediaId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "MediaUploaded processing cancelled. MediaId={MediaId}",
                msg.MediaId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create analysis job. MediaId={MediaId}",
                msg.MediaId);

            throw;
        }
    }
}