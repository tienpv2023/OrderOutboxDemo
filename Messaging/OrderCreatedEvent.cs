namespace OrderOutboxDemo.Messaging;

public record OrderCreatedEvent(
    Guid MessageId,
    Guid OrderId,
    string CustomerEmail,
    decimal TotalAmount,
    DateTime CreatedAtUtc
);