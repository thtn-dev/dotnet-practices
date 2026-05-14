using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using MetaWebhook.Shared;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Stream.Client;
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

var consumer = await Consumer.Create(new ConsumerConfig(streamSystem, rabbitMqOptions.SuperStreamName)
{
    Reference = rabbitMqOptions.ConsumerReference,
    ClientProvidedName = $"meta-webhook-subscriber-{rabbitMqOptions.InstanceId}",
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
        var messageBody = ReadMessageBody(message);
        var webhookEvent = JsonSerializer.Deserialize<MetaWebhookEvent>(messageBody, jsonOptions);
        if (webhookEvent is null)
        {
            Console.WriteLine(
                $"INVALID_JSON instanceId={rabbitMqOptions.InstanceId} stream={stream} offset={context.Offset}");
            return;
        }

        var idempotencyKey = $"meta:{webhookEvent.PageId}:{webhookEvent.SenderId}:{webhookEvent.MessageId}";
        var processedAt = DateTimeOffset.UtcNow;

        if (!processedMessages.TryAdd(idempotencyKey, 0))
        {
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
                $"processedAt={processedAt:O}");

            // Duplicates are safe to skip, then commit so they are not replayed forever.
            await rawConsumer.StoreOffset(context.Offset);
            return;
        }

        if (subscriberOptions.SimulateMs > 0)
        {
            await Task.Delay(subscriberOptions.SimulateMs);
        }

        if (Random.Shared.NextDouble() < subscriberOptions.FailRate)
        {
            Console.WriteLine(
                "FAILED " +
                $"instanceId={rabbitMqOptions.InstanceId} " +
                $"stream={stream} " +
                $"offset={context.Offset} " +
                $"pageId={webhookEvent.PageId} " +
                $"senderId={webhookEvent.SenderId} " +
                $"conversationId={webhookEvent.ConversationId} " +
                $"messageId={webhookEvent.MessageId} " +
                $"text=\"{webhookEvent.Text}\" " +
                $"processedAt={DateTimeOffset.UtcNow:O}");

            return;
        }

        Console.WriteLine(
            "PROCESSED " +
            $"instanceId={rabbitMqOptions.InstanceId} " +
            $"stream={stream} " +
            $"offset={context.Offset} " +
            $"pageId={webhookEvent.PageId} " +
            $"senderId={webhookEvent.SenderId} " +
            $"conversationId={webhookEvent.ConversationId} " +
            $"messageId={webhookEvent.MessageId} " +
            $"text=\"{webhookEvent.Text}\" " +
            $"processedAt={DateTimeOffset.UtcNow:O}");

        // Manual offset tracking: commit only after successful processing.
        await rawConsumer.StoreOffset(context.Offset);
    }
});

try
{
    Console.WriteLine(
        $"Subscriber started. instanceId={rabbitMqOptions.InstanceId} " +
        $"superStream={rabbitMqOptions.SuperStreamName} " +
        $"consumerReference={rabbitMqOptions.ConsumerReference} " +
        $"from={subscriberOptions.From.ToString().ToLowerInvariant()}");

    await shutdown.Task;
}
finally
{
    Console.WriteLine($"Shutting down subscriber instanceId={rabbitMqOptions.InstanceId}");
    await consumer.Close();
    await streamSystem.Close();
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
    public int SimulateMs { get; private init; } = 50;
    public double FailRate { get; private init; }
    public OffsetStart From { get; private init; } = OffsetStart.First;

    public static SubscriberOptions Parse(string[] args)
    {
        var values = Cli.Read(args);

        return new SubscriberOptions
        {
            InstanceId = values.GetValueOrDefault("instance-id"),
            ConsumerReference = values.GetValueOrDefault("consumer-reference"),
            SimulateMs = Cli.GetInt(values, "simulate-ms", 500, min: 0),
            FailRate = Cli.GetDouble(values, "fail-rate", 0, min: 0, max: 1),
            From = Cli.GetOffsetStart(values, "from", OffsetStart.First)
        };
    }
}

internal enum OffsetStart
{
    First,
    Next
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

        if (!double.TryParse(rawValue, out var value) || value < min || value > max)
        {
            throw new ArgumentException($"--{key} must be a decimal between {min} and {max}.");
        }

        return value;
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
}
