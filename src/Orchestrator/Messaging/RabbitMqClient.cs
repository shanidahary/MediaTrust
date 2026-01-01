using System.Text.Json;
using RabbitMQ.Client;

namespace MediaTrust.Orchestrator.Messaging;

public sealed class RabbitMqClient
{
    private readonly IConnection _conn;
    private readonly ILogger<RabbitMqClient> _logger;

    public const string Exchange = "detectors.exchange";
    public const string Queue = "detector.basic.queue";
    public const string RoutingKey = "detector.basic";

    public RabbitMqClient(IConfiguration cfg, ILogger<RabbitMqClient> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = cfg["RabbitMq:Host"] ?? "rabbitmq"
        };

        _conn = factory.CreateConnection();
    }

    public void Publish(object msg)
    {
        try
        {
            using var ch = _conn.CreateModel();

            ch.ExchangeDeclare(Exchange, ExchangeType.Direct, durable: true);
            ch.QueueDeclare(Queue, durable: true, exclusive: false, autoDelete: false);
            ch.QueueBind(Queue, Exchange, RoutingKey);

            var body = JsonSerializer.SerializeToUtf8Bytes(msg);

            ch.BasicPublish(
                exchange: Exchange,
                routingKey: RoutingKey,
                basicProperties: null,
                body: body);

            _logger.LogInformation("Published message to {Queue}", Queue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rabbit publish failed");
            throw;
        }
    }
}