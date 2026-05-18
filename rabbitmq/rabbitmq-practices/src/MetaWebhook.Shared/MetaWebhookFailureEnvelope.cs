namespace MetaWebhook.Shared;

public sealed class MetaWebhookFailureEnvelope
{
    public MetaWebhookEvent Event { get; set; } = new();
    public int Attempt { get; set; }
    public string OriginalStream { get; set; } = string.Empty;
    public ulong OriginalOffset { get; set; }
    public string LastFailureReason { get; set; } = string.Empty;
    public DateTimeOffset FirstFailedAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class MetaWebhookDlqEnvelope
{
    public MetaWebhookEvent Event { get; set; } = new();
    public int Attempts { get; set; }
    public string SourceStream { get; set; } = string.Empty;
    public ulong SourceOffset { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public DateTimeOffset FirstFailedAt { get; set; }
    public DateTimeOffset DeadLetteredAt { get; set; }
}
