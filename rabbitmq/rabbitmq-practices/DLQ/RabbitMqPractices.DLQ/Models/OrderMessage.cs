

namespace RabbitMqPractices.DLQ.Models;

public class OrderMessage
{
    public Guid OrderId { get; set; } = Guid.NewGuid();
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool SimulateError { get; set; } = false;   // set true to force DLQ

    public override string ToString()
        => $"[Order {OrderId:N}] Customer={CustomerName}, Amount={Amount:C}, Error={SimulateError}";
}
