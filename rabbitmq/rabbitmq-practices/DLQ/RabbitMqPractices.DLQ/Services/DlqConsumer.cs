using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqPractices.DLQ.Config;
using RabbitMqPractices.DLQ.Infrastructure;
using RabbitMqPractices.DLQ.Models;
using System.Text;

namespace RabbitMqPractices.DLQ.Services;

public sealed class DlqConsumer(
    IRabbitMQConnection conn,
    ILogger<DlqConsumer> logger) : IAsyncDisposable
{
    private readonly IChannel _channel = conn.CreateChannel().GetAwaiter().GetResult();

    public async Task Start(CancellationToken ct = default)
    {
        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += OnDlqMessageAsync;

        await _channel.BasicConsumeAsync(
            queue: RabbitMQConstants.DlqQueue,
            autoAck: false,
            consumerTag: "dlq-consumer",
            consumer: consumer,
            cancellationToken: ct);

        logger.LogWarning(
            "☠️ DLQ Consumer started on queue '{Queue}'.",
            RabbitMQConstants.DlqQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (TaskCanceledException)
        {
            logger.LogInformation("🛑 DLQ Consumer stopping.");
        }
    }

    // ─────────────────────────────────────────────────────────────

    private async Task OnDlqMessageAsync(
        object sender,
        BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);

            var message = JsonConvert.DeserializeObject<OrderMessage>(json);

            var deathInfo = GetDeathInfo(ea.BasicProperties);

            logger.LogError(
                """
                💀 DLQ message received.
                   OrderId    : {OrderId}
                   Customer   : {Customer}
                   DLQ Reason : {Reason}
                   Origin Q   : {Queue}
                   Deaths     : {Count}
                """,
                message?.OrderId,
                message?.CustomerName,
                deathInfo.Reason,
                deathInfo.Queue,
                deathInfo.Count);

            // TODO:
            // - persist to DB
            // - send alert email/slack
            // - trigger compensating action
            // - move to quarantine storage

            await _channel.BasicAckAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error processing DLQ message – discarding.");

            await _channel.BasicNackAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                requeue: false);
        }
    }

    // ─────────────────────────────────────────────────────────────

    private static (string Reason, string Queue, long Count)
        GetDeathInfo(IReadOnlyBasicProperties props)
    {
        try
        {
            if (props.Headers != null &&
                props.Headers.TryGetValue("x-death", out var deathObj) &&
                deathObj is IList<object> { Count: > 0 } deaths)
            {
                if (deaths[0] is Dictionary<string, object> death)
                {
                    var reasonObj = death.GetValueOrDefault("reason");
                    var reasonBytes = reasonObj as byte[] ?? [];
                    var reason = Encoding.UTF8.GetString(reasonBytes);
                    
                    var queueObj  = death.GetValueOrDefault("queue");
                    var  queueBytes = queueObj as byte[] ?? [];
                    var  queue = Encoding.UTF8.GetString(queueBytes);
                    return (
                        Reason: reason,
                        Queue: queue,
                        Count:
                            death.TryGetValue("count", out var c) && c is long l
                                ? l
                                : 0
                    );
                }
            }
        }
        catch
        {
            // ignore malformed x-death headers
        }

        return ("unknown", "unknown", 0);
    }

    public ValueTask DisposeAsync()
        => _channel.DisposeAsync();
}