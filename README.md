# OrderOutboxDemo
# OrderOutboxDemo

## Overview

**OrderOutboxDemo** is a .NET 8 Web API project demonstrating the **Transactional Outbox Pattern** with SQL Server, RabbitMQ, and Background Services.

The project ensures that order data and event messages are saved in the same database transaction before being published to RabbitMQ for asynchronous processing.

## Architecture

```text
Client
  |
  v
.NET Web API
  |
  v
SQL Server Transaction
  |-- Orders
  |-- OutboxMessages
  |
  v
Outbox Background Worker
  |
  v
RabbitMQ
  |
  v
Order Consumer
  |-- Send Email
  |-- Create Invoice
```

## Key Features

- **Transactional Outbox Pattern:** Saves orders and outbox messages atomically.
- **Background Processing:** Publishes pending outbox messages asynchronously.
- **RabbitMQ Integration:** Enables asynchronous communication between services.
- **Event-Driven Architecture:** Processes order-created events independently of the API request.
- **Separation of Concerns:** Separates order creation, message publishing, and event consumption.

## Technologies

- C# / .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- RabbitMQ
- BackgroundService
- Swagger / OpenAPI

## How It Works

1. The client sends a request to create an order.
2. The API saves the order and an outbox message in a single database transaction.
3. A background worker reads pending outbox messages.
4. The worker publishes events to RabbitMQ.
5. A consumer processes the events and performs tasks such as sending emails and creating invoices.

## Getting Started

1. Install the .NET 8 SDK.
2. Set up SQL Server and RabbitMQ.
3. Configure the database connection string in `appsettings.json`.
4. Configure RabbitMQ connection settings.
5. Apply Entity Framework Core migrations.
6. Run the application and test the API through Swagger.

## Learning Objectives

This project demonstrates practical knowledge of:

- Building REST APIs with ASP.NET Core
- Database transactions and Entity Framework Core
- Reliable asynchronous messaging
- Background workers and message queues
- Event-driven system design
- The Transactional Outbox Pattern

## Future Improvements

- Add retry policies and dead-letter queues.
- Implement idempotent message processing.
- Add structured logging and monitoring.
- Improve outbox message locking and concurrent processing.
- Add integration tests for the API, database, and RabbitMQ.

## License

Add a license file if you intend to distribute this project publicly.
