namespace RabbitMqPractices.DLQ.Config;

public static class RabbitMQConstants
{
    // ─── Main Exchange & Queue ─────────────────────────────────────────────────
    public const string MainExchange = "demo.exchange";
    public const string MainQueue = "demo.queue";
    public const string MainRoutingKey = "demo.routing.key";

    // ─── Dead Letter Exchange & Queue ─────────────────────────────────────────
    public const string DlxExchange = "demo.dlx.exchange";
    public const string DlqQueue = "demo.dlq";
    public const string DlqRoutingKey = "demo.dlq.routing.key";

    // ─── Retry settings ───────────────────────────────────────────────────────
    public const int MaxRetryCount = 3;
    public const string RetryCountHeader = "x-retry-count";
}