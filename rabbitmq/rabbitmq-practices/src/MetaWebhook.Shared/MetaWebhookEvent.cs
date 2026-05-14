namespace MetaWebhook.Shared;

public sealed class MetaWebhookEvent
{
    public string Platform { get; set; } = "meta";
    public string PageId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Type { get; set; } = "message";
    public string Text { get; set; } = string.Empty;
    public string RawJson { get; set; } = string.Empty;
}
