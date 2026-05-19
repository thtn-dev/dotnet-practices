using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMqPractices.DLQ.Config;
using RabbitMqPractices.DLQ.Infrastructure;
using RabbitMqPractices.DLQ.Models;
using System.Text;

namespace RabbitMqPractices.DLQ.Services;

public interface IMessagePublisher
{
    Task Publish(OrderMessage message, CancellationToken ct = default);
    Task PublishBatch(IEnumerable<OrderMessage> messages, CancellationToken ct = default);
}


public sealed class MessagePublisher(IRabbitMQConnection conn, ILogger<MessagePublisher> logger)
    : IMessagePublisher, IAsyncDisposable
{
    private readonly IChannel _channel = conn.CreateConfirmChannel().GetAwaiter().GetResult();

    public async Task Publish(OrderMessage message, CancellationToken ct = default)
    {
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = message.OrderId.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                { RabbitMQConstants.RetryCountHeader, 0 }
            }
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        await _channel.BasicPublishAsync(
            exchange: RabbitMQConstants.MainExchange,
            routingKey: RabbitMQConstants.MainRoutingKey,
            mandatory: true,
            basicProperties: props,
            body: body,
            cancellationToken: cts.Token);

        logger.LogInformation("📤 Published: {Message}", message);
    }

    public async Task PublishBatch(IEnumerable<OrderMessage> messages, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        foreach (var msg in messages)
        {
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));

            var props = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = msg.OrderId.ToString(),
                Headers = new Dictionary<string, object?>
                {
                    { RabbitMQConstants.RetryCountHeader, 0 }
                }
            };

            await _channel.BasicPublishAsync(
                exchange: RabbitMQConstants.MainExchange,
                routingKey: RabbitMQConstants.MainRoutingKey,
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: cts.Token);
        }

        logger.LogInformation("📤 Batch published successfully.");
    }

    public ValueTask DisposeAsync() => _channel.DisposeAsync();

}