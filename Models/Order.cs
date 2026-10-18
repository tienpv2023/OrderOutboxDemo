namespace OrderOutboxDemo.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CustomerEmail { get; set; } = "";

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;
}