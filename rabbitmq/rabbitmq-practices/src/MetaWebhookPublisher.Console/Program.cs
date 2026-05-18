using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using MetaWebhook.Shared;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.AMQP;
using RabbitMQ.Stream.Client.Reliable;

var publisherOptions = PublisherOptions.Parse(args);
var rabbitMqOptions = LoadRabbitMqOptions();
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = false
};

await using var streamSystem = await StreamSystem.Create(new StreamSystemConfig
{
    UserName = rabbitMqOptions.RabbitMqUser,
    Password = rabbitMqOptions.RabbitMqPassword,
    VirtualHost = rabbitMqOptions.VirtualHost,
    Endpoints = [await ResolveEndpoint(rabbitMqOptions.RabbitMqHost, rabbitMqOptions.RabbitMqPort)],
    ClientProvidedName = $"meta-webhook-publisher-{Environment.MachineName}"
});

if (publisherOptions.CreateSuperStream)
{
    await EnsureSuperStream(streamSystem, rabbitMqOptions);
}

var pendingConfirms = new ConcurrentDictionary<string, TaskCompletionSource<ConfirmationStatus>>();
var producer = await Producer.Create(new ProducerConfig(streamSystem, rabbitMqOptions.SuperStreamName)
{
    ClientProvidedName = $"meta-webhook-publisher-{Environment.MachineName}",
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

try
{
    var events = await LoadEvents(publisherOptions);
    if (events.Count == 0)
    {
        Console.WriteLine("No Meta webhook events to publish.");
        return;
    }

    Console.WriteLine(
        $"Publishing {events.Count} event(s) to super stream '{rabbitMqOptions.SuperStreamName}' with routing key = conversationId.");

    for (var index = 0; index < events.Count; index++)
    {
        var webhookEvent = events[index];
        var correlationId = $"{webhookEvent.MessageId}:{index}:{Guid.NewGuid():N}";
        var payload = JsonSerializer.SerializeToUtf8Bytes(webhookEvent, jsonOptions);

        // The routing callback reads MessageId, so keep the routing key as conversationId.
        var message = new Message(payload)
        {
            Properties = new Properties
            {
                MessageId = webhookEvent.ConversationId,
                CorrelationId = correlationId,
                ContentType = "application/json",
                CreationTime = DateTime.UtcNow
            },
        };

        var confirm = new TaskCompletionSource<ConfirmationStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingConfirms[correlationId] = confirm;

        await producer.Send(message);

        var status = await WaitForConfirm(correlationId, confirm.Task, pendingConfirms);
        Console.WriteLine(
            "PUBLISHED " +
            $"index={index + 1} " +
            $"messageId={webhookEvent.MessageId} " +
            $"conversationId={webhookEvent.ConversationId} " +
            $"confirm={status}");

        if (publisherOptions.DelayMs > 0)
        {
            await Task.Delay(publisherOptions.DelayMs);
        }
    }
}
finally
{
    await producer.Close();
    await streamSystem.Close();
}

return;

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

static async Task EnsureSuperStream(StreamSystem streamSystem, RabbitMqStreamOptions options)
{
    if (await streamSystem.SuperStreamExists(options.SuperStreamName))
    {
        Console.WriteLine($"Super stream '{options.SuperStreamName}' already exists.");
        return;
    }

    await streamSystem.CreateSuperStream(new PartitionsSuperStreamSpec(options.SuperStreamName, options.Partitions));
    Console.WriteLine($"Created super stream '{options.SuperStreamName}' with {options.Partitions} partition(s).");
}

static async Task<IReadOnlyList<MetaWebhookEvent>> LoadEvents(PublisherOptions options)
{
    if (!string.IsNullOrWhiteSpace(options.File))
    {
        var rawJson = await File.ReadAllTextAsync(options.File);
        return MetaWebhookParser.Parse(rawJson);
    }

    return GenerateFakeEvents(options.Count, options.Users);
}

static IReadOnlyList<MetaWebhookEvent> GenerateFakeEvents(int count, int users)
{
    var events = new List<MetaWebhookEvent>(count);
    const string pageId = "PAGE_100001";
    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    for (var i = 1; i <= count; i++)
    {
        var userNumber = ((i - 1) % users) + 1;
        var senderId = $"USER_{userNumber:000000}";
        var messageId = $"m_{senderId}_{i:000000}";

        events.Add(new MetaWebhookEvent
        {
            Platform = "meta",
            PageId = pageId,
            SenderId = senderId,
            RecipientId = pageId,
            ConversationId = $"meta:{pageId}:{senderId}",
            MessageId = messageId,
            Timestamp = now + i,
            Type = "message",
            Text = $"fake message {i} from {senderId}",
            RawJson = string.Empty
        });
    }

    return events;
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

internal sealed class PublisherOptions
{
    public string? File { get; private init; }
    public int Count { get; private init; } = 100;
    public int Users { get; private init; } = 10;
    public int DelayMs { get; private init; }
    public bool CreateSuperStream { get; private init; }

    public static PublisherOptions Parse(string[] args)
    {
        var values = Cli.Read(args);

        return new PublisherOptions
        {
            File = values.GetValueOrDefault("file"),
            Count = Cli.GetInt(values, "count", 100, min: 1),
            Users = Cli.GetInt(values, "users", 10, min: 1),
            DelayMs = Cli.GetInt(values, "delay-ms", 0, min: 0),
            CreateSuperStream = Cli.GetBool(values, "create-super-stream", true)
        };
    }
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

    public static bool GetBool(Dictionary<string, string> values, string key, bool defaultValue)
    {
        return values.TryGetValue(key, out var rawValue)
            ? bool.TryParse(rawValue, out var value) && value
            : defaultValue;
    }
}
