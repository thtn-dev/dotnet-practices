using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMqPractices.DLQ.Infrastructure;
using RabbitMqPractices.DLQ.Models;
using RabbitMqPractices.DLQ.Services;
using RabbitMqPractices.Shared;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var services = new ServiceCollection();
services.AddLogging(b => b
    .AddConsole()
    .SetMinimumLevel(LogLevel.Information));

services.Configure<RabbitMqOptions>(
    configuration.GetSection("RabbitMQ"));

services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
services.AddSingleton<QueueTopology>();
services.AddTransient<MessagePublisher>();
services.AddTransient<MessageConsumer>();
services.AddTransient<DlqConsumer>();

await using var provider = services.BuildServiceProvider();

await provider.GetRequiredService<QueueTopology>().DeclareAll();
var rabbitMqSettings = provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

Console.WriteLine("""
 ╔══════════════════════════════════════════╗
 ║       RabbitMQ Demo  –  C# / .NET 8      ║
 ╠══════════════════════════════════════════╣
 ║  1  Publish a normal message             ║
 ║  2  Publish a message that forces DLQ    ║
 ║  3  Publish a batch (3 normal + 1 error) ║
 ║  4  Start Main Consumer                  ║
 ║  5  Start DLQ Consumer                   ║
 ║  q  Quit                                 ║
 ╚══════════════════════════════════════════╝
""");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };


while (true)
{
    Console.Write("Select option > ");
    var key = Console.ReadLine()?.Trim().ToLower();

    if (key == "q") break;

    switch (key)
    {
        case "1":
            {
                var pub = provider.GetRequiredService<MessagePublisher>();
                await pub.Publish(new OrderMessage
                {
                    CustomerName = "Nguyen Van A",
                    Amount = 1_500_000,
                    SimulateError = false,
                });
                break;
            }

        case "2":
            {
                var pub = provider.GetRequiredService<MessagePublisher>();
                await pub.Publish(new OrderMessage
                {
                    CustomerName = "Tran Thi B",
                    Amount = 750_000,
                    SimulateError = true,   // will exhaust retries → DLQ
                });
                break;
            }

        case "3":
            {
                var pub = provider.GetRequiredService<MessagePublisher>();
                await pub.PublishBatch(
                [
                    new OrderMessage { CustomerName = "Le Van C",  Amount = 200_000, SimulateError = true },
                    new OrderMessage { CustomerName = "Pham Thi D", Amount = 350_000 },
                    new OrderMessage { CustomerName = "Hoang Van E", Amount = 500_000 },
                    new OrderMessage { CustomerName = "Vo Thi F",   Amount = 100_000, SimulateError = true },
                ]);
                break;
            }

        case "4":
            {
                Console.WriteLine("Starting Main Consumer (Ctrl+C to stop)…");
                var consumer = provider.GetRequiredService<MessageConsumer>();
                // Run in background so menu stays responsive
                await Task.Run(() => consumer.Start(cts.Token));
                break;
            }

        case "5":
            {
                Console.WriteLine("Starting DLQ Consumer (Ctrl+C to stop)…");
                var dlq = provider.GetRequiredService<DlqConsumer>();
                await Task.Run(() => dlq.Start(cts.Token));
                break;
            }

        default:
            Console.WriteLine("Unknown option.");
            break;
    }
}

Console.WriteLine("Goodbye 👋");