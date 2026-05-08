using System.Buffers;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using RabbitMqPractices.Shared;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var services = new ServiceCollection();

services.Configure<RabbitMqOptions>(
    configuration.GetSection("RabbitMQ"));

await using var provider = services.BuildServiceProvider();

var rabbitMq = provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

var config = new StreamSystemConfig()
{
    UserName = rabbitMq.UserName,
    Password = rabbitMq.Password,
    Endpoints = new List<EndPoint>() { new IPEndPoint(IPAddress.Loopback, 5552) }
};

var streamSystem = await StreamSystem.Create(config);

Console.WriteLine("Connected to RabbitMQ Stream!");

var consumer = await Consumer.Create(new ConsumerConfig(streamSystem, "nathan-test-stream")
{
    Reference = $"consumer-{Guid.NewGuid()}",
    OffsetSpec = new OffsetTypeFirst(),
    MessageHandler = async (stream, consumer, context, message) =>
    {
        var bytes = new byte[message.Data.Contents.Length];
        message.Data.Contents.CopyTo(bytes);
        var messageBody = Encoding.UTF8.GetString(bytes);
        
        var payload = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(messageBody);
        
        Console.WriteLine($"Received: {payload.GetProperty("test").GetString()} " +
                          $"at {payload.GetProperty("timestamp").GetInt64()}");
        await Task.CompletedTask;
    }
});
Console.WriteLine("Consumer started. Waiting for messages...");
Console.ReadLine();

await consumer.Close();
await streamSystem.Close();