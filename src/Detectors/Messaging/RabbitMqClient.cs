using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using MediaTrust.Detectors.Models;

namespace MediaTrust.Detectors.Messaging;

public sealed class RabbitMqClient : IDisposable
{
    private readonly IConnection _conn;
    private readonly IModel _channel;

    public const string Exchange = "detectors.exchange";
    public const string Queue = "detector.basic.queue";
    public const string RoutingKey = "detector.basic";

    public RabbitMqClient(IConfiguration cfg)
    {
        var factory = new ConnectionFactory
        {
            HostName = cfg["RabbitMq:Host"] ?? "rabbitmq"
        };

        // Try several times in case RabbitMQ is still starting
        _conn = CreateConnectionWithRetry(factory);

        _channel = _conn.CreateModel();

        _channel.ExchangeDeclare(
            Exchange,
            ExchangeType.Direct,
            durable: true);

        _channel.QueueDeclare(
            Queue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            Queue,
            Exchange,
            RoutingKey);
    }

    private static IConnection CreateConnectionWithRetry(
        ConnectionFactory factory)
    {
        const int maxRetries = 10;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return factory.CreateConnection();
            }
            catch (BrokerUnreachableException)
                when (attempt < maxRetries)
            {
                Thread.Sleep(
                    TimeSpan.FromSeconds(3));
            }
        }

        throw new InvalidOperationException(
            "Could not connect to RabbitMQ after multiple attempts.");
    }

    public PulledDetectorMessage? Pull()
    {
        var res = _channel.BasicGet(
            Queue,
            autoAck: false);

        if (res == null)
            return null;

        var request =
            JsonSerializer.Deserialize<DetectorRequest>(
                res.Body.Span);

        if (request == null)
        {
            _channel.BasicReject(
                res.DeliveryTag,
                requeue: false);

            return null;
        }

        return new PulledDetectorMessage(
            request,
            res.DeliveryTag);
    }

    public void Ack(ulong deliveryTag)
    {
        _channel.BasicAck(
            deliveryTag,
            multiple: false);
    }

    public void Nack(
        ulong deliveryTag,
        bool requeue = true)
    {
        _channel.BasicNack(
            deliveryTag,
            multiple: false,
            requeue: requeue);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _conn.Dispose();
    }
}

public sealed record PulledDetectorMessage(
    DetectorRequest Request,
    ulong DeliveryTag);