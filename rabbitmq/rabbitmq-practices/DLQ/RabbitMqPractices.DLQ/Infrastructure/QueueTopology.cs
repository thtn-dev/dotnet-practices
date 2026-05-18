using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMqPractices.DLQ.Config;


namespace RabbitMqPractices.DLQ.Infrastructure;

/// <summary>
/// Declares all exchanges, queues and bindings required by the demo.
///
/// Topology:
///
///   Producer
///      │
///      ▼
///  [demo.exchange]  ──(demo.routing.key)──►  [demo.queue]
///                                                  │
///                                       x-dead-letter-exchange
///                                                  │
///                                                  ▼
///                                         [demo.dlx.exchange] ──►  [demo.dlq]
/// </summary>
public class QueueTopology
{
    private readonly IRabbitMQConnection _conn;
    private readonly ILogger<QueueTopology> _logger;

    public QueueTopology(IRabbitMQConnection conn, ILogger<QueueTopology> logger)
    {
        _conn = conn;
        _logger = logger;
    }

    public async Task DeclareAll()
    {
        using var channel = await _conn.CreateChannel();

        // 1. Dead-Letter Exchange (fanout – routes everything to DLQ)
        await channel.ExchangeDeclareAsync(
            exchange: RabbitMQConstants.DlxExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // 2. Dead-Letter Queue
        await channel.QueueDeclareAsync(
            queue: RabbitMQConstants.DlqQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await channel.QueueBindAsync(
            queue: RabbitMQConstants.DlqQueue,
            exchange: RabbitMQConstants.DlxExchange,
            routingKey: RabbitMQConstants.DlqRoutingKey);

        // 3. Main Exchange
        await channel.ExchangeDeclareAsync(
            exchange: RabbitMQConstants.MainExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // 4. Main Queue – with DLQ wired in
        var mainQueueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange",    RabbitMQConstants.DlxExchange },
            { "x-dead-letter-routing-key", RabbitMQConstants.DlqRoutingKey },
            // Optional: message TTL (30 s) – expired messages also go to DLQ
             { "x-message-ttl", 30_000 },
        };

        await channel.QueueDeclareAsync(
            queue: RabbitMQConstants.MainQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: mainQueueArgs);

        await channel.QueueBindAsync(
            queue: RabbitMQConstants.MainQueue,
            exchange: RabbitMQConstants.MainExchange,
            routingKey: RabbitMQConstants.MainRoutingKey);

        _logger.LogInformation("Queue topology declared successfully.");
    }
}
