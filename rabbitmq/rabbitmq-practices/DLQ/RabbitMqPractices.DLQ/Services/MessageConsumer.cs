using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqPractices.DLQ.Config;
using RabbitMqPractices.DLQ.Infrastructure;
using RabbitMqPractices.DLQ.Models;
using System.Text;

namespace RabbitMqPractices.DLQ.Services;

public sealed class MessageConsumer : IAsyncDisposable
{
    private readonly IChannel _channel;
    private readonly ILogger<MessageConsumer> _logger;

    public MessageConsumer(
        IRabbitMQConnection conn,
        ILogger<MessageConsumer> logger)
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

        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: RabbitMQConstants.MainQueue,
            autoAck: false,
            consumerTag: "demo-consumer",
            consumer: consumer,
            cancellationToken: ct);

        _logger.LogInformation(
            "👂 Consumer started on queue '{Queue}'. Waiting for messages…",
            RabbitMQConstants.MainQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("🛑 Consumer stopping.");
        }
    }


    private async Task OnMessageReceivedAsync(
        object sender,
        BasicDeliverEventArgs ea)
    {
        OrderMessage? message = null;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);

            message = JsonConvert.DeserializeObject<OrderMessage>(json)
                      ?? throw new InvalidOperationException(
                          "Deserialization returned null.");

            _logger.LogInformation(
                "📥 Received: {Message}",
                message);

            await ProcessMessage(message);

            await _channel.BasicAckAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false);

            _logger.LogInformation(
                "✅ ACK – message {OrderId} processed.",
                message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "⚠️ Error processing message.");

            await HandleFailure(ea, ex);
        }
    }

    // ─────────────────────────────────────────────────────────────

    private static async Task ProcessMessage(OrderMessage message)
    {
        // simulate processing time
        await Task.Delay(500);

        // simulate failure
        if (message.SimulateError)
        {
            throw new Exception(
                "Simulated processing error – will retry then send to DLQ.");
        }

        Console.WriteLine(
            $"💰 Processing order for {message.CustomerName}: {message.Amount:C}");
    }

    // ─────────────────────────────────────────────────────────────

    private async Task HandleFailure(
        BasicDeliverEventArgs ea,
        Exception ex)
    {
        var retryCount = GetRetryCount(ea.BasicProperties);

        if (retryCount < RabbitMQConstants.MaxRetryCount)
        {
            await RetryMessage(ea, retryCount + 1);

            // ACK old message
            await _channel.BasicAckAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false);

            _logger.LogWarning(
                "🔄 Retry {Attempt}/{Max} scheduled.",
                retryCount + 1,
                RabbitMQConstants.MaxRetryCount);
        }
        else
        {
            // send to DLQ
            await _channel.BasicNackAsync(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                requeue: false);

            _logger.LogError(
                "💀 Max retries reached. Message sent to DLQ. Error: {Error}",
                ex.Message);
        }
    }

    private async Task RetryMessage(
        BasicDeliverEventArgs ea,
        int newRetryCount)
    {
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Headers = ea.BasicProperties.Headers ??
                      new Dictionary<string, object?>()
        };

        props.Headers[RabbitMQConstants.RetryCountHeader] =
            newRetryCount;

        await _channel.BasicPublishAsync(
            exchange: RabbitMQConstants.MainExchange,
            routingKey: RabbitMQConstants.MainRoutingKey,
            mandatory: true,
            basicProperties: props,
            body: ea.Body);
    }

    private static int GetRetryCount(
        IReadOnlyBasicProperties props)
    {
        if (props.Headers != null &&
            props.Headers.TryGetValue(
                RabbitMQConstants.RetryCountHeader,
                out var val))
        {
            return val switch
            {
                int i => i,
                long l => (int)l,
                byte[] b => BitConverter.ToInt32(b, 0),
                _ => 0
            };
        }

        return 0;
    }

    public ValueTask DisposeAsync()
        => _channel.DisposeAsync();
}