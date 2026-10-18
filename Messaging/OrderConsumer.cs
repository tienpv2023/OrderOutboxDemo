using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderOutboxDemo.Messaging;

public class OrderConsumer(
    IConfiguration configuration,
    ILogger<OrderConsumer> logger)
    : BackgroundService
{
    private const string QueueName = "orders.created";

    protected override Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration["RabbitMQ:Uri"]
                ?? "amqp://guest:guest@localhost:5672/"),
            DispatchConsumersAsync = false
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                var message =
                    JsonSerializer.Deserialize<OrderCreatedEvent>(json)
                    ?? throw new InvalidOperationException(
                        "Invalid OrderCreated event");

                SendEmail(message);
                CreateInvoice(message);

                // ACK chỉ khi toàn bộ xử lý thành công.
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Order consumer failed");

                // Demo: đưa message lỗi về cuối queue.
                // Production nên có retry giới hạn và dead-letter queue.
                channel.BasicNack(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: false);
            }
        };

        channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        stoppingToken.Register(() =>
        {
            channel.Dispose();
            connection.Dispose();
        });

        logger.LogInformation("Order consumer started");

        return Task.CompletedTask;
    }

    private void SendEmail(OrderCreatedEvent message)
    {
        // Demo: thay bằng SMTP hoặc email provider thực tế.
        logger.LogInformation(
            "[EMAIL] Sent confirmation to {Email} for order {OrderId}",
            message.CustomerEmail,
            message.OrderId);
    }

    private void CreateInvoice(OrderCreatedEvent message)
    {
        // Demo: thay bằng logic tạo hóa đơn thật.
        logger.LogInformation(
            "[INVOICE] Created invoice for order {OrderId}, amount {Amount}",
            message.OrderId,
            message.TotalAmount);
    }
}