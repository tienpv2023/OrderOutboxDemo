using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderOutboxDemo.Data;
using OrderOutboxDemo.Messaging;
using OrderOutboxDemo.Models;

namespace OrderOutboxDemo.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(AppDbContext db) : ControllerBase
{
    public record CreateOrderRequest(
        string CustomerEmail,
        decimal TotalAmount
    );

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerEmail) ||
            request.TotalAmount <= 0)
        {
            return BadRequest("Email và số tiền không hợp lệ.");
        }

        var order = new Order
        {
            CustomerEmail = request.CustomerEmail,
            TotalAmount = request.TotalAmount
        };

        var message = new OrderCreatedEvent(
            MessageId: Guid.NewGuid(),
            OrderId: order.Id,
            CustomerEmail: order.CustomerEmail,
            TotalAmount: order.TotalAmount,
            CreatedAtUtc: order.CreatedAtUtc
        );

        var outbox = new OutboxMessage
        {
            Id = message.MessageId,
            Type = nameof(OrderCreatedEvent),
            Payload = JsonSerializer.Serialize(message)
        };

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        db.Orders.Add(order);
        db.OutboxMessages.Add(outbox);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Accepted(new
        {
            order.Id,
            order.TotalAmount,
            Status = "OrderCreated_OutboxPending"
        });
    }
}