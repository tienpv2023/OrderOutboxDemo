namespace OrderOutboxDemo.Models;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Type { get; set; } = "";

    public string Payload { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;

    public DateTime? ProcessedAtUtc { get; set; }

    public int RetryCount { get; set; }

    public string? Error { get; set; }
}