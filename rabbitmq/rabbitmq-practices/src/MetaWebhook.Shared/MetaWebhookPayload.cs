using System.Text.Json.Serialization;

namespace MetaWebhook.Shared;

public sealed class MetaWebhookPayload
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("entry")]
    public List<MetaEntry> Entry { get; set; } = [];
}

public sealed class MetaEntry
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("time")]
    public long? Time { get; set; }

    [JsonPropertyName("messaging")]
    public List<MetaMessaging> Messaging { get; set; } = [];
}

public sealed class MetaMessaging
{
    [JsonPropertyName("sender")]
    public MetaUserRef? Sender { get; set; }

    [JsonPropertyName("recipient")]
    public MetaUserRef? Recipient { get; set; }

    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }

    [JsonPropertyName("message")]
    public MetaMessage? Message { get; set; }
}

public sealed class MetaUserRef
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public sealed class MetaMessage
{
    [JsonPropertyName("mid")]
    public string? Mid { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
