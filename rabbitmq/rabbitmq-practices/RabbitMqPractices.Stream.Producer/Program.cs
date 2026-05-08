using System.Net;
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
await using var scope = provider.CreateAsyncScope();

var rabbitMq = provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

var config = new StreamSystemConfig()
{
    UserName = rabbitMq.UserName,
    Password = rabbitMq.Password,
    Endpoints = new List<EndPoint>() {new IPEndPoint(IPAddress.Loopback, 5552)}
};

var streamSystem = await StreamSystem.Create(config);

var streamSpec = new StreamSpec("nathan-test-stream")
{
    MaxAge = TimeSpan.FromHours(1),
    MaxLengthBytes = 100_000_000, // 100MB max
};

await streamSystem.CreateStream(streamSpec);

var producer = await Producer.Create(new ProducerConfig(streamSystem, "nathan-test-stream")
{
    Reference = $"producer-{Guid.NewGuid()}",
    MaxInFlight = 1000,
});

while (true)
{
    var message = new
    {
        test = "Hello RabbitMQ Stream!",
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    };
    
    Console.WriteLine($"Publishing message: {message.test} at {message.timestamp}");

    var payload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);
    var rabbitMessage = new Message(payload);
    await producer.Send(rabbitMessage);
    await Task.Delay(1000);
}