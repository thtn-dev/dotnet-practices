using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqPractices.DLQ.Config;
using RabbitMqPractices.DLQ.Infrastructure;
using RabbitMqPractices.DLQ.Models;
using System.Text;

namespace RabbitMqPractices.DLQ.Services;

/// <summary>
/// Consumes messages from the Dead Letter Queue.
/// You can use this to:
///   - Log / alert on failed messages
///   - Store them in DB for manual reprocessing
///   - Trigger recovery workflows
/// </summary>
public sealed class DlqConsumer : IAsyncDisposable
{
    private readonly IChannel _channel;
    private readonly ILogger<DlqConsumer> _logger;

    public DlqConsumer(
        IRabbitMQConnection conn,
        ILogger<DlqConsumer> logger)
    {
        _logger = logger;
        _channel = conn.CreateChannel().GetAwaiter().GetResult();
    }

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

        _logger.LogWarning(
            "☠️ DLQ Consumer started on queue '{Queue}'.",
            RabbitMQConstants.DlqQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("🛑 DLQ Consumer stopping.");
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

            _logger.LogError(
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
            _logger.LogError(
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
                deathObj is IList<object> deaths &&
                deaths.Count > 0)
            {
                var death = deaths[0] as Dictionary<string, object>;

                if (death is not null)
                {
                    return (
                        Reason:
                            death.TryGetValue("reason", out var r)
                                ? r?.ToString() ?? "unknown"
                                : "unknown",

                        Queue:
                            death.TryGetValue("queue", out var q)
                                ? q?.ToString() ?? "unknown"
                                : "unknown",

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