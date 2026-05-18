namespace MetaWebhook.Shared;

public sealed class RabbitMqStreamOptions
{
    public string RabbitMqHost { get; set; } = "127.0.0.1";
    public int RabbitMqPort { get; set; } = 5552;
    public string RabbitMqUser { get; set; } = "guest";
    public string RabbitMqPassword { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string SuperStreamName { get; set; } = "meta.webhook.raw";
    public string RetrySuperStreamName { get; set; } = "meta.webhook.retry";
    public string DlqSuperStreamName { get; set; } = "meta.webhook.dlq";
    public int Partitions { get; set; } = 8;
    public string ConsumerReference { get; set; } = "webhook-normalizer";
    public string InstanceId { get; set; } = $"instance-{Environment.MachineName}-{Guid.NewGuid():N}"[..32];
}
