using System.Text.Json;

namespace MetaWebhook.Shared;

public static class MetaWebhookParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<MetaWebhookEvent> Parse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return [];
        }

        var payload = JsonSerializer.Deserialize<MetaWebhookPayload>(rawJson, SerializerOptions);
        if (payload?.Entry is null || payload.Entry.Count == 0)
        {
            return [];
        }

        var events = new List<MetaWebhookEvent>();
        foreach (var entry in payload.Entry)
        {
            foreach (var messaging in entry.Messaging)
            {
                var pageId = entry.Id ?? messaging.Recipient?.Id ?? string.Empty;
                var senderId = messaging.Sender?.Id ?? string.Empty;
                var recipientId = messaging.Recipient?.Id ?? pageId;
                var messageId = messaging.Message?.Mid ?? string.Empty;

                if (string.IsNullOrWhiteSpace(pageId) ||
                    string.IsNullOrWhiteSpace(senderId) ||
                    string.IsNullOrWhiteSpace(messageId))
                {
                    continue;
                }

                events.Add(new MetaWebhookEvent
                {
                    Platform = "meta",
                    PageId = pageId,
                    SenderId = senderId,
                    RecipientId = recipientId,
                    ConversationId = $"meta:{pageId}:{senderId}",
                    MessageId = messageId,
                    Timestamp = messaging.Timestamp ?? entry.Time ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Type = messaging.Message is null ? "unknown" : "message",
                    Text = messaging.Message?.Text ?? string.Empty,
                    RawJson = rawJson
                });
            }
        }

        return events;
    }
}
