using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using OrderOutboxDemo.Data;

namespace OrderOutboxDemo.Messaging;

public class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OutboxWorker> logger)
    : BackgroundService
{
    private const string QueueName = "orders.created";

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox worker failed");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var messages = await db.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration["RabbitMQ:Uri"]
                ?? "amqp://guest:guest@localhost:5672/")
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Chờ RabbitMQ xác nhận publish.
        channel.ConfirmSelect();

        foreach (var message in messages)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message.Payload);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = message.Id.ToString();
                properties.Type = message.Type;

                channel.BasicPublish(
                    exchange: "",
                    routingKey: QueueName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);

                // Chỉ đánh dấu processed sau khi broker xác nhận.
                channel.WaitForConfirmsOrDie(
                    TimeSpan.FromSeconds(5));

                message.ProcessedAtUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                logger.LogError(ex,
                    "Failed to publish Outbox {MessageId}",
                    message.Id);

                // Dừng batch để tránh lặp vô hạn trong cùng lượt.
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}