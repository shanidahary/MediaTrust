using System.Text.Json;
using RabbitMQ.Client;

namespace MediaTrust.Orchestrator.Messaging;

public sealed class RabbitMqClient
{
    private readonly IConnection _conn;
    private readonly ILogger<RabbitMqClient> _logger;

    public const string Exchange = "detectors.exchange";

    public RabbitMqClient(IConfiguration cfg, ILogger<RabbitMqClient> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = cfg["RabbitMq:Host"] ?? "rabbitmq"
        };

        _conn = factory.CreateConnection();
    }

    public void Publish(string routingKey, object msg)
    {
        try
        {
            using var ch = _conn.CreateModel();
            ch.ExchangeDeclare(Exchange, ExchangeType.Direct, durable: true);

            var body = JsonSerializer.SerializeToUtf8Bytes(msg);
            ch.BasicPublish(Exchange, routingKey, null, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rabbit publish failed");
            throw;
        }
    }
}
