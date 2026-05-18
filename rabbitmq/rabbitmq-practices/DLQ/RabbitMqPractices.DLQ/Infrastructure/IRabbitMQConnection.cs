using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMqPractices.Shared;

namespace RabbitMqPractices.DLQ.Infrastructure;

public interface IRabbitMQConnection : IAsyncDisposable
{
    Task<IChannel> CreateChannel();
    Task<IChannel> CreateConfirmChannel();
    bool IsConnected { get; }
}

public sealed class RabbitMQConnection : IRabbitMQConnection
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQConnection> _logger;
    private bool _disposed;

    public bool IsConnected => _connection.IsOpen && !_disposed;

    public RabbitMQConnection(IOptions<RabbitMqOptions> rbSettings, ILogger<RabbitMQConnection> logger)
    {
        _logger = logger;
        var settings = rbSettings.Value;

        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
            // Auto-recovery
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _logger.LogInformation("✅ RabbitMQ connected to {Host}:{Port}", settings.HostName, settings.Port);
    }

    public Task<IChannel> CreateChannel()
    {
        if (!IsConnected)
            throw new InvalidOperationException("RabbitMQ connection is not open.");

        return _connection.CreateChannelAsync();
    }
    public Task<IChannel> CreateConfirmChannel()
    {
        if (!IsConnected)
            throw new InvalidOperationException("RabbitMQ connection is not open.");

        var opts = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        return _connection.CreateChannelAsync(opts);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        return new ValueTask(_connection.CloseAsync());
    }
}