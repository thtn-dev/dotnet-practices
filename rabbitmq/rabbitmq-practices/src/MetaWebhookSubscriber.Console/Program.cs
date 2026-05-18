using System.Buffers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using MetaWebhook.Shared;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.AMQP;
using RabbitMQ.Stream.Client.Reliable;

var subscriberOptions = SubscriberOptions.Parse(args);
var rabbitMqOptions = LoadRabbitMqOptions();

if (!string.IsNullOrWhiteSpace(subscriberOptions.InstanceId))
{
    rabbitMqOptions.InstanceId = subscriberOptions.InstanceId;
}

if (!string.IsNullOrWhiteSpace(subscriberOptions.ConsumerReference))
{
    rabbitMqOptions.ConsumerReference = subscriberOptions.ConsumerReference;
}

var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.TrySetResult();
};

var processedMessages = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

await using var streamSystem = await StreamSystem.Create(new StreamSystemConfig
{
    UserName = rabbitMqOptions.RabbitMqUser,
    Password = rabbitMqOptions.RabbitMqPassword,
    VirtualHost = rabbitMqOptions.VirtualHost,
    Endpoints = [await ResolveEndpoint(rabbitMqOptions.RabbitMqHost, rabbitMqOptions.RabbitMqPort)],
    ClientProvidedName = $"meta-webhook-subscriber-{rabbitMqOptions.InstanceId}"
});

if (subscriberOptions.CreateSupportStreams)
{
    await EnsureSuperStream(streamSystem, rabbitMqOptions.RetrySuperStreamName, rabbitMqOptions.Partitions);
    await EnsureSuperStream(streamSystem, rabbitMqOptions.DlqSuperStreamName, rabbitMqOptions.Partitions);
}

var pendingConfirms = new ConcurrentDictionary<string, TaskCompletionSource<ConfirmationStatus>>();
var retryProducer = await CreateSuperStreamProducer(
    streamSystem,
    rabbitMqOptions.RetrySuperStreamName,
    $"meta-webhook-retry-producer-{rabbitMqOptions.InstanceId}",
    pendingConfirms);
var dlqProducer = await CreateSuperStreamProducer(
    streamSystem,
    rabbitMqOptions.DlqSuperStreamName,
    $"meta-webhook-dlq-producer-{rabbitMqOptions.InstanceId}",
    pendingConfirms);

var sourceStreamName = subscriberOptions.Source == SubscriberSource.Raw
    ? rabbitMqOptions.SuperStreamName
    : subscriberOptions.Source == SubscriberSource.Retry
        ? rabbitMqOptions.RetrySuperStreamName
        : rabbitMqOptions.DlqSuperStreamName;

var consumer = await Consumer.Create(new ConsumerConfig(streamSystem, sourceStreamName)
{
    Reference = rabbitMqOptions.ConsumerReference,
    ClientProvidedName = $"meta-webhook-subscriber-{rabbitMqOptions.InstanceId}-{subscriberOptions.Source.ToString().ToLowerInvariant()}",
    IsSuperStream = true,
    IsSingleActiveConsumer = true,
    OffsetSpec = subscriberOptions.From == OffsetStart.First ? new OffsetTypeFirst() : new OffsetTypeNext(),
    ConsumerUpdateListener = async (stream, reference, isActive) =>
    {
        Console.WriteLine(
            $"SAC_UPDATE instanceId={rabbitMqOptions.InstanceId} stream={stream} reference={reference} active={isActive}");

        if (!isActive)
        {
            return new OffsetTypeNext();
        }

        var storedOffset = await streamSystem.TryQueryOffset(reference, stream);
        if (storedOffset.HasValue)
        {
            // Offsets are stored after processing, so resume from the next message.
            return new OffsetTypeOffset(storedOffset.Value + 1);
        }

        return subscriberOptions.From == OffsetStart.First ? new OffsetTypeFirst() : new OffsetTypeNext();
    },
    MessageHandler = async (stream, rawConsumer, context, message) =>
    {
        if (subscriberOptions.Source == SubscriberSource.Raw)
        {
            await HandleRawMessage(
                stream,
                rawConsumer,
                context,
                message,
                rabbitMqOptions,
                subscriberOptions,
                processedMessages,
                retryProducer,
                dlqProducer,
                pendingConfirms,
                jsonOptions);
            return;
        }

        if (subscriberOptions.Source == SubscriberSource.Retry)
        {
            await HandleRetryMessage(
                stream,
                rawConsumer,
                context,
                message,
                rabbitMqOptions,
                subscriberOptions,
                processedMessages,
                retryProducer,
                dlqProducer,
                pendingConfirms,
                jsonOptions,
                shutdown.Task);
            return;
        }

        await HandleDlqMessage(stream, rawConsumer, context, message, rabbitMqOptions, jsonOptions);
    }
});

try
{
    Console.WriteLine(
        $"Subscriber started. instanceId={rabbitMqOptions.InstanceId} " +
        $"source={subscriberOptions.Source.ToString().ToLowerInvariant()} " +
        $"superStream={sourceStreamName} " +
        $"retryStream={rabbitMqOptions.RetrySuperStreamName} " +
        $"dlqStream={rabbitMqOptions.DlqSuperStreamName} " +
        $"consumerReference={rabbitMqOptions.ConsumerReference} " +
        $"from={subscriberOptions.From.ToString().ToLowerInvariant()} " +
        $"failRate={subscriberOptions.FailRate} " +
        $"dlqRate={subscriberOptions.DlqRate} " +
        $"retryTtlMs={subscriberOptions.RetryTtlMs} " +
        $"maxRetryAttempts={subscriberOptions.MaxRetryAttempts}");

    await shutdown.Task;
}
finally
{
    Console.WriteLine($"Shutting down subscriber instanceId={rabbitMqOptions.InstanceId}");
    await consumer.Close();
    await retryProducer.Close();
    await dlqProducer.Close();
    await streamSystem.Close();
}

static async Task HandleRawMessage(
    string stream,
    RawConsumer rawConsumer,
    MessageContext context,
    Message message,
    RabbitMqStreamOptions rabbitMqOptions,
    SubscriberOptions subscriberOptions,
    ConcurrentDictionary<string, byte> processedMessages,
    Producer retryProducer,
    Producer dlqProducer,
    ConcurrentDictionary<string, TaskCompletionSource<ConfirmationStatus>> pendingConfirms,
    JsonSerializerOptions jsonOptions)
{
    var messageBody = ReadMessageBody(message);
    var webhookEvent = JsonSerializer.Deserialize<MetaWebhookEvent>(messageBody, jsonOptions);
    if (webhookEvent is null)
    {
        Console.WriteLine($"INVALID_JSON instanceId={rabbitMqOptions.InstanceId} stream={stream} offset={context.Offset}");
        await rawConsumer.StoreOffset(context.Offset);
        return;
    }

    if (await HandleDuplicate(stream, rawConsumer, context, rabbitMqOptions, processedMessages, webhookEvent))
    {
        return;
    }

    if (subscriberOptions.SimulateMs > 0)
    {
        await Task.Delay(subscriberOptions.SimulateMs);
    }

    var failureReason = PickFailureReason(subscriberOptions);
    if (failureReason == FailureReason.Permanent)
    {
        await PublishDlq(
            dlqProducer,
            pendingConfirms,
            new MetaWebhookDlqEnvelope
            {
                Event = webhookEvent,
                Attempts = 1,
                SourceStream = stream,
                SourceOffset = context.Offset,
                FailureReason = "simulated permanent failure",
                FirstFailedAt = DateTimeOffset.UtcNow,
                DeadLetteredAt = DateTimeOffset.UtcNow
            },
            jsonOptions);

        Console.WriteLine(
            "DLQ " +
            $"instanceId={rabbitMqOptions.InstanceId} stream={stream} offset={context.Offset} " +
            $"messageId={webhookEvent.MessageId} conversationId={webhookEvent.ConversationId} reason=\"simulated permanent failure\"");
        await rawConsumer.StoreOffset(context.Offset);
        return;
    }

    if (failureReason == FailureReason.Retryable)
    {
        var failedAt = DateTimeOffset.UtcNow;
        await PublishRetry(
            retryProducer,
            pendingConfirms,
            new MetaWebhookFailureEnvelope
            {
                Event = webhookEvent,
                Attempt = 1,
                OriginalStream = stream,
                OriginalOffset = context.Offset,
                LastFailureReason = "simulated retryable failure",
                FirstFailedAt = failedAt,
                AvailableAt = failedAt.AddMilliseconds(subscriberOptions.RetryTtlMs),
                CreatedAt = DateTimeOffset.UtcNow
            },
            jsonOptions);

        Console.WriteLine(
            "RETRY_SCHEDULED " +
            $"instanceId={rabbitMqOptions.InstanceId} stream={stream} offset={context.Offset} " +
            $"messageId={webhookEvent.MessageId} conversationId={webhookEvent.ConversationId} " +
            $"attempt=1 availableAt={failedAt.AddMilliseconds(subscriberOptions.RetryTtlMs):O}");
        await rawConsumer.StoreOffset(context.Offset);
        return;
    }

    LogProcessed(stream, context.Offset, rabbitMqOptions.InstanceId, webhookEvent);
    processedMessages.TryAdd(BuildIdempotencyKey(webhookEvent), 0);
    // Manual offset tracking: commit only after successful processing.
    await rawConsumer.StoreOffset(context.Offset);
}

static async Task HandleRetryMessage(
    string stream,
    RawConsumer rawConsumer,
    MessageContext context,
    Message message,
    RabbitMqStreamOptions rabbitMqOptions,
    SubscriberOptions subscriberOptions,
    ConcurrentDictionary<string, byte> processedMessages,
    Producer retryProducer,
    Producer dlqProducer,
    ConcurrentDictionary<string, TaskCompletionSource<ConfirmationStatus>> pendingConfirms,
    JsonSerializerOptions jsonOptions,
    Task shutdownTask)
{
    var messageBody = ReadMessageBody(message);
    var retry = JsonSerializer.Deserialize<MetaWebhookFailureEnvelope>(messageBody, jsonOptions);
    if (retry?.Event is null)
    {
        Console.WriteLine($"INVALID_RETRY_JSON instanceId={rabbitMqOptions.InstanceId} stream={stream} offset={context.Offset}");
        await rawConsumer.StoreOffset(context.Offset);
        return;
    }

    var delay = retry.AvailableAt - DateTimeOffset.UtcNow;
    if (delay > TimeSpan.Zero)
    {
        Console.WriteLine(
            "RETRY_WAIT " +
            $"instanceId={rabbitMqOptions.InstanceId} stream={stream} offset={context.Offset} " +
            $"messageId={retry.Event.MessageId} attempt={retry.Attempt} availableAt={retry.AvailableAt:O} waitMs={(long)delay.TotalMilliseconds}");

        var completed = await Task.WhenAny(Task.Delay(delay), shutdownTask);
        if (completed == shutdownTask)
        {
            return;
        }
    }

    if (await HandleDuplicate(stream, rawConsumer, context, rabbitMqOptions, processedMessages, retry.Event))
    {
        return;
    }

    if (subscriberOptions.SimulateMs > 0)
    {
        await Task.Delay(subscriberOptions.SimulateMs);
    }

    var failureReason = PickFailureReason(subscriberOptions);
    if (failureReason == FailureReason.None)
    {
        LogProcessed(stream, context.Offset, rabbitMqOptions.InstanceId, retry.Event);
        processedMessages.TryAdd(BuildIdempotencyKey(retry.Event), 0);
        await rawConsumer.StoreOffset(context.Offset);
        return;
    }

    var nextAttempt = retry.Attempt + 1;
    if (failureReason == FailureReason.Permanent || nextAttempt > subscriberOptions.MaxRetryAttempts)
    {
        var reason = failureReason == FailureReason.Permanent
            ? "simulated permanent failure"
            : $"retry attempts exhausted after {retry.Attempt} attempt(s)";
        await PublishDlq(
            dlqProducer,
            pendingConfirms,
            new MetaWebhookDlqEnvelope
            {
                Event = retry.Event,
                Attempts = retry.Attempt,
                SourceStream = stream,
                SourceOffset = context.Offset,
                FailureReason = reason,
                FirstFailedAt = retry.FirstFailedAt,
                DeadLetteredAt = DateTimeOffset.UtcNow
            },
            jsonOptions);

        Console.WriteLine(
            "DLQ " +
            $"instanceId={rabbitMqOptions.InstanceId} stream={stream} offset={context.Offset} " +
            $"messageId={retry.Event.MessageId} conversationId={retry.Event.ConversationId} attempts={retry.Attempt} reason=\"{reason}\"");
        await rawConsumer.StoreOffset(context.Offset);
        return;
    }

    var failedAt = DateTimeOffset.UtcNow;
    await PublishRetry(
        retryProducer,
        pendingConfirms,
        new MetaWebhookFailureEnvelope
        {
            Event = retry.Event,
            Attempt = nextAttempt,
            OriginalStream = retry.OriginalStream,
            OriginalOffset = retry.OriginalOffset,
            LastFailureReason = "simulated retryable failure",
            FirstFailedAt = retry.FirstFailedAt,
            AvailableAt = failedAt.AddMilliseconds(subscriberOptions.RetryTtlMs),
            CreatedAt = DateTimeOffset.UtcNow
        },
        jsonOptions);

    Console.WriteLine(
        "RETRY_RESCHEDULED " +
        $"instanceId={rabbitMqOptions.InstanceId} stream={stream} offset={context.Offset} " +
        $"messageId={retry.Event.MessageId} conversationId={retry.Event.ConversationId} " +
        $"attempt={nextAttempt} availableAt={failedAt.AddMilliseconds(subscriberOptions.RetryTtlMs):O}");
    await rawConsumer.StoreOffset(context.Offset);
}

static async Task HandleDlqMessage(
    string stream,
    RawConsumer rawConsumer,
    MessageContext context,
    Message message,
    RabbitMqStreamOptions rabbitMqOptions,
    JsonSerializerOptions jsonOptions)
{
    var messageBody = ReadMessageBody(message);
    var dlq = JsonSerializer.Deserialize<MetaWebhookDlqEnvelope>(messageBody, jsonOptions);
    if (dlq?.Event is null)
    {
        Console.WriteLine($"INVALID_DLQ_JSON instanceId={rabbitMqOptions.InstanceId} stream={stream} offset={context.Offset}");
        await rawConsumer.StoreOffset(context.Offset);
        return;
    }

    Console.WriteLine(
        "DLQ_RECEIVED " +
        $"instanceId={rabbitMqOptions.InstanceId} " +
        $"stream={stream} " +
        $"offset={context.Offset} " +
        $"sourceStream={dlq.SourceStream} " +
        $"sourceOffset={dlq.SourceOffset} " +
        $"attempts={dlq.Attempts} " +
        $"pageId={dlq.Event.PageId} " +
        $"senderId={dlq.Event.SenderId} " +
        $"conversationId={dlq.Event.ConversationId} " +
        $"messageId={dlq.Event.MessageId} " +
        $"reason=\"{dlq.FailureReason}\" " +
        $"deadLetteredAt={dlq.DeadLetteredAt:O}");

    await rawConsumer.StoreOffset(context.Offset);
}

static async Task<bool> HandleDuplicate(
    string stream,
    RawConsumer rawConsumer,
    MessageContext context,
    RabbitMqStreamOptions rabbitMqOptions,
    ConcurrentDictionary<string, byte> processedMessages,
    MetaWebhookEvent webhookEvent)
{
    if (!processedMessages.ContainsKey(BuildIdempotencyKey(webhookEvent)))
    {
        return false;
    }

    Console.WriteLine(
        "DUPLICATE " +
        $"instanceId={rabbitMqOptions.InstanceId} " +
        $"stream={stream} " +
        $"offset={context.Offset} " +
        $"pageId={webhookEvent.PageId} " +
        $"senderId={webhookEvent.SenderId} " +
        $"conversationId={webhookEvent.ConversationId} " +
        $"messageId={webhookEvent.MessageId} " +
        $"text=\"{webhookEvent.Text}\" " +
        $"processedAt={DateTimeOffset.UtcNow:O}");

    // Duplicates are safe to skip, then commit so they are not replayed forever.
    await rawConsumer.StoreOffset(context.Offset);
    return true;
}

static async Task<Producer> CreateSuperStreamProducer(
    StreamSystem streamSystem,
    string superStreamName,
    string clientProvidedName,
    ConcurrentDictionary<string, TaskCompletionSource<ConfirmationStatus>> pendingConfirms)
{
    return await Producer.Create(new ProducerConfig(streamSystem, superStreamName)
    {
        ClientProvidedName = clientProvidedName,
        MaxInFlight = 10_000,
        SuperStreamConfig = new SuperStreamConfig
        {
            Enabled = true,
            RoutingStrategyType = RoutingStrategyType.Hash,
            Routing = message => message.Properties.MessageId?.ToString() ?? string.Empty
        },
        ConfirmationHandler = confirmation =>
        {
            foreach (var message in confirmation.Messages)
            {
                if (message.Properties.CorrelationId?.ToString() is { Length: > 0 } correlationId &&
                    pendingConfirms.TryRemove(correlationId, out var completion))
                {
                    completion.TrySetResult(confirmation.Status);
                }
            }

            return Task.CompletedTask;
        }
    });
}

static async Task PublishRetry(
    Producer retryProducer,
    ConcurrentDictionary<string, TaskCompletionSource<ConfirmationStatus>> pendingConfirms,
    MetaWebhookFailureEnvelope retry,
    JsonSerializerOptions jsonOptions)
{
    await PublishEnvelope(retryProducer, pendingConfirms, retry.Event.ConversationId, retry, jsonOptions);
}

static async Task PublishDlq(
    Producer dlqProducer,
    ConcurrentDictionary<string, TaskCompletionSource<ConfirmationStatus>> pendingConfirms,
    MetaWebhookDlqEnvelope dlq,
    JsonSerializerOptions jsonOptions)
{
    await PublishEnvelope(dlqProducer, pendingConfirms, dlq.Event.ConversationId, dlq, jsonOptions);
}

static async Task PublishEnvelope<T>(
    Producer producer,
    ConcurrentDictionary<string, TaskCompletionSource<ConfirmationStatus>> pendingConfirms,
    string routingKey,
    T envelope,
    JsonSerializerOptions jsonOptions)
{
    var correlationId = $"{routingKey}:{Guid.NewGuid():N}";
    var confirm = new TaskCompletionSource<ConfirmationStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
    pendingConfirms[correlationId] = confirm;

    var message = new Message(JsonSerializer.SerializeToUtf8Bytes(envelope, jsonOptions))
    {
        Properties = new Properties
        {
            MessageId = routingKey,
            CorrelationId = correlationId,
            ContentType = "application/json",
            CreationTime = DateTime.UtcNow
        }
    };

    await producer.Send(message);

    var status = await WaitForConfirm(correlationId, confirm.Task, pendingConfirms);
    if (status != ConfirmationStatus.Confirmed)
    {
        throw new InvalidOperationException($"Failed to publish envelope. routingKey={routingKey} status={status}");
    }
}

static async Task<ConfirmationStatus> WaitForConfirm(
    string correlationId,
    Task<ConfirmationStatus> confirmTask,
    ConcurrentDictionary<string, TaskCompletionSource<ConfirmationStatus>> pendingConfirms)
{
    var completed = await Task.WhenAny(confirmTask, Task.Delay(TimeSpan.FromSeconds(30)));
    if (completed == confirmTask)
    {
        return await confirmTask;
    }

    pendingConfirms.TryRemove(correlationId, out _);
    return ConfirmationStatus.ClientTimeoutError;
}

static void LogProcessed(string stream, ulong offset, string instanceId, MetaWebhookEvent webhookEvent)
{
    Console.WriteLine(
        "PROCESSED " +
        $"instanceId={instanceId} " +
        $"stream={stream} " +
        $"offset={offset} " +
        $"pageId={webhookEvent.PageId} " +
        $"senderId={webhookEvent.SenderId} " +
        $"conversationId={webhookEvent.ConversationId} " +
        $"messageId={webhookEvent.MessageId} " +
        $"text=\"{webhookEvent.Text}\" " +
        $"processedAt={DateTimeOffset.UtcNow:O}");
}

static string BuildIdempotencyKey(MetaWebhookEvent webhookEvent)
{
    return $"meta:{webhookEvent.PageId}:{webhookEvent.SenderId}:{webhookEvent.MessageId}";
}

static FailureReason PickFailureReason(SubscriberOptions options)
{
    var value = Random.Shared.NextDouble();
    if (value < options.DlqRate)
    {
        return FailureReason.Permanent;
    }

    return value < options.DlqRate + options.FailRate ? FailureReason.Retryable : FailureReason.None;
}

static RabbitMqStreamOptions LoadRabbitMqOptions()
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddEnvironmentVariables("META_WEBHOOK_")
        .Build();

    var options = new RabbitMqStreamOptions();
    configuration.GetSection("RabbitMQ").Bind(options);
    configuration.Bind(options);
    return options;
}

static async Task<IPEndPoint> ResolveEndpoint(string host, int port)
{
    if (IPAddress.TryParse(host, out var ipAddress))
    {
        return new IPEndPoint(ipAddress, port);
    }

    var addresses = await Dns.GetHostAddressesAsync(host);
    var address = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                  ?? addresses.First();
    return new IPEndPoint(address, port);
}

static async Task EnsureSuperStream(StreamSystem streamSystem, string superStreamName, int partitions)
{
    if (await streamSystem.SuperStreamExists(superStreamName))
    {
        return;
    }

    await streamSystem.CreateSuperStream(new PartitionsSuperStreamSpec(superStreamName, partitions));
    Console.WriteLine($"Created super stream '{superStreamName}' with {partitions} partition(s).");
}

static string ReadMessageBody(Message message)
{
    var bytes = new byte[message.Data.Contents.Length];
    message.Data.Contents.CopyTo(bytes);
    return Encoding.UTF8.GetString(bytes);
}

internal sealed class SubscriberOptions
{
    public string? InstanceId { get; private init; }
    public string? ConsumerReference { get; private init; }
    public int SimulateMs { get; private init; } = 500;
    public double FailRate { get; private init; } = 0.02;
    public double DlqRate { get; private init; } = 0.005;
    public int RetryTtlMs { get; private init; } = 5000;
    public int MaxRetryAttempts { get; private init; } = 3;
    public bool CreateSupportStreams { get; private init; } = true;
    public OffsetStart From { get; private init; } = OffsetStart.First;
    public SubscriberSource Source { get; private init; } = SubscriberSource.Raw;

    public static SubscriberOptions Parse(string[] args)
    {
        var values = Cli.Read(args);
        var failRate = Cli.GetDouble(values, "fail-rate", 0.02, min: 0, max: 1);
        var dlqRate = Cli.GetDouble(values, "dlq-rate", 0.005, min: 0, max: 1);
        if (failRate + dlqRate > 1)
        {
            throw new ArgumentException("--fail-rate + --dlq-rate must be <= 1.");
        }

        return new SubscriberOptions
        {
            InstanceId = values.GetValueOrDefault("instance-id"),
            ConsumerReference = values.GetValueOrDefault("consumer-reference"),
            SimulateMs = Cli.GetInt(values, "simulate-ms", 500, min: 0),
            FailRate = failRate,
            DlqRate = dlqRate,
            RetryTtlMs = Cli.GetInt(values, "retry-ttl-ms", 5000, min: 0),
            MaxRetryAttempts = Cli.GetInt(values, "max-retry-attempts", 3, min: 1),
            CreateSupportStreams = Cli.GetBool(values, "create-support-streams", true),
            From = Cli.GetOffsetStart(values, "from", OffsetStart.First),
            Source = Cli.GetSubscriberSource(values, "source", SubscriberSource.Raw)
        };
    }
}

internal enum OffsetStart
{
    First,
    Next
}

internal enum SubscriberSource
{
    Raw,
    Retry,
    Dlq
}

internal enum FailureReason
{
    None,
    Retryable,
    Permanent
}

internal static class Cli
{
    public static Dictionary<string, string> Read(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = args[i][2..];
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
            values[key] = value;
        }

        return values;
    }

    public static int GetInt(Dictionary<string, string> values, string key, int defaultValue, int min)
    {
        if (!values.TryGetValue(key, out var rawValue))
        {
            return defaultValue;
        }

        if (!int.TryParse(rawValue, out var value) || value < min)
        {
            throw new ArgumentException($"--{key} must be an integer >= {min}.");
        }

        return value;
    }

    public static double GetDouble(Dictionary<string, string> values, string key, double defaultValue, double min, double max)
    {
        if (!values.TryGetValue(key, out var rawValue))
        {
            return defaultValue;
        }

        if (!double.TryParse(rawValue, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var value) || value < min || value > max)
        {
            throw new ArgumentException($"--{key} must be a decimal between {min} and {max}.");
        }

        return value;
    }

    public static bool GetBool(Dictionary<string, string> values, string key, bool defaultValue)
    {
        return values.TryGetValue(key, out var rawValue)
            ? bool.TryParse(rawValue, out var value) && value
            : defaultValue;
    }

    public static OffsetStart GetOffsetStart(Dictionary<string, string> values, string key, OffsetStart defaultValue)
    {
        if (!values.TryGetValue(key, out var rawValue))
        {
            return defaultValue;
        }

        return rawValue.Equals("next", StringComparison.OrdinalIgnoreCase)
            ? OffsetStart.Next
            : OffsetStart.First;
    }

    public static SubscriberSource GetSubscriberSource(
        Dictionary<string, string> values,
        string key,
        SubscriberSource defaultValue)
    {
        if (!values.TryGetValue(key, out var rawValue))
        {
            return defaultValue;
        }

        if (rawValue.Equals("retry", StringComparison.OrdinalIgnoreCase))
        {
            return SubscriberSource.Retry;
        }

        return rawValue.Equals("dlq", StringComparison.OrdinalIgnoreCase)
            ? SubscriberSource.Dlq
            : SubscriberSource.Raw;
    }
}
