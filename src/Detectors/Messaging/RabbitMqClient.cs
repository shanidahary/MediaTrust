using System.Text.Json;
using RabbitMQ.Client;
using MediaTrust.Detectors.Models;

namespace MediaTrust.Detectors.Messaging;

public sealed class RabbitMqClient
{
    private readonly IConnection _conn;

    public const string Exchange = "detectors.exchange";
    public const string Queue = "detector.basic.queue";
    public const string RoutingKey = "detector.basic";

    public RabbitMqClient(IConfiguration cfg)
    {
        var factory = new ConnectionFactory
        {
            HostName = cfg["RabbitMq:Host"] ?? "rabbitmq"
        };
        _conn = factory.CreateConnection();
    }

    public DetectorRequest? Pull()
    {
        using var ch = _conn.CreateModel();

        ch.ExchangeDeclare(Exchange, ExchangeType.Direct, true);
        ch.QueueDeclare(Queue, true, false, false);
        ch.QueueBind(Queue, Exchange, RoutingKey);

        var res = ch.BasicGet(Queue, true);
        return res == null
            ? null
            : JsonSerializer.Deserialize<DetectorRequest>(res.Body.Span);
    }
}
