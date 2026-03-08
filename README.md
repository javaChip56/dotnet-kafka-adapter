# dotnet-kafka-adapter

Lightweight .NET Kafka adapter intended to let an existing codebase publish and consume Kafka messages without taking a direct dependency on Kafka-specific APIs throughout the application.

## Goal

Provide a small, maintainable adapter library that:

- Exposes application-facing abstractions for publishing and consuming messages
- Wraps Kafka client setup, configuration, serialization, and delivery concerns
- Integrates cleanly with standard .NET dependency injection and hosting
- Keeps application code insulated from Kafka implementation details

## Initial Scope

The first version should focus on the smallest useful feature set:

- Typed message publishing
- Typed message consumption
- JSON serialization and deserialization
- DI registration for producer and consumer services
- Background consumer hosting for worker services or ASP.NET Core apps
- Basic logging, error handling, and retry behavior

## Non-Goals

The first version does not need to solve everything:

- Full schema registry support
- Complex stream processing
- Exactly-once delivery guarantees
- Custom admin tooling
- Broad broker management features

## Status

This repository now contains the initial solution scaffold, public contracts, producer and consumer implementations, DI registration, typed handler registration helpers, retry/dead-letter behavior, a runnable sample app, and live integration tests against a local Kafka broker.

## Checklist

### Done

- [x] Confirmed the library is technically feasible as a lightweight .NET adapter over Kafka
- [x] Defined the high-level goal and initial project scope
- [x] Created the initial project README
- [x] Created the .NET solution and class library structure
- [x] Chosen the initial target framework (`net8.0`)
- [x] Defined the first-pass public abstractions for publishing and consuming
- [x] Defined the first-pass configuration model for brokers, topics, auth, and consumer groups
- [x] Added the Kafka client dependency (`Confluent.Kafka`)
- [x] Implemented the first producer wrapper for typed messages
- [x] Added the first JSON serialization path for produced messages
- [x] Added DI registration extensions for the producer and consumer paths
- [x] Implemented the consumer worker pattern for typed handlers
- [x] Added JSON deserialization support for consumers
- [x] Added typed handler registration helpers for consuming apps
- [x] Added structured logging around produce/consume failures
- [x] Decided the initial offset commit strategy and failure semantics
- [x] Added retry behavior and dead-letter strategy
- [x] Added local development setup for Kafka
- [x] Added integration tests against a local Kafka instance
- [x] Added sample application showing publish/consume usage
- [x] Added usage documentation and configuration examples

### To Do

- [ ] Refine the public API surface as the adapter hardens
- [ ] Expand documentation for non-local broker authentication and production guidance

## Proposed Deliverables

The likely shape of the library:

- A reusable adapter package
- A small sample app
- Integration tests
- Documentation for setup and usage

## Local Kafka

The repository includes a single-node Kafka stack for local development in [docker-compose.kafka.yml](D:\Research\dotnet-kafka-adapter\docker-compose.kafka.yml).

### Start Kafka

```bash
docker compose -f docker-compose.kafka.yml up -d
```

Kafka will be reachable from the host on `localhost:9092`.

### Stop Kafka

```bash
docker compose -f docker-compose.kafka.yml down
```

To remove the Kafka data volume as well:

```bash
docker compose -f docker-compose.kafka.yml down -v
```

### Notes

- This uses a single broker in KRaft combined mode for local development only.
- The adapter should use `localhost:9092` as `BootstrapServers` when running on the host machine.

## Integration Tests

Run the Kafka integration tests against the local broker with:

```bash
dotnet test tests/DotNetKafkaAdapter.IntegrationTests/DotNetKafkaAdapter.IntegrationTests.csproj
```

## Sample App

A runnable sample lives in [samples/DotNetKafkaAdapter.SampleApp](D:\Research\dotnet-kafka-adapter\samples\DotNetKafkaAdapter.SampleApp).

Run it against the local broker with:

```bash
dotnet run --project samples/DotNetKafkaAdapter.SampleApp/DotNetKafkaAdapter.SampleApp.csproj
```

Optional environment variables:

- `KAFKA_BOOTSTRAP_SERVERS` defaults to `localhost:9092`
- `KAFKA_TOPIC` defaults to `sample.orders`
- `KAFKA_CONSUMER_GROUP` defaults to `sample.orders.consumer`
- `KAFKA_DEAD_LETTER_TOPIC` defaults to `<topic>.dlq`

The sample app:

- creates the sample topic and dead-letter topic if they do not exist
- starts the adapter consumer hosted service
- publishes one `OrderSubmitted` message
- logs the consumed message and stops the host

## Usage

Typical registration in an application:

```csharp
using DotNetKafkaAdapter.Abstractions;
using Microsoft.Extensions.DependencyInjection;

services.AddKafkaAdapter(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.ClientId = "my-app";
    options.Producer.DefaultTopic = "orders";
});

services.AddKafkaHandler<OrderSubmitted, OrderSubmittedHandler>(
    topic: "orders",
    consumerGroup: "orders-consumer",
    registration =>
    {
        registration.MaxRetryAttempts = 3;
        registration.RetryDelay = TimeSpan.FromSeconds(1);
        registration.DeadLetterTopic = "orders.dlq";
    });
```

Publish a message:

```csharp
await publisher.PublishAsync(
    "orders",
    new OrderSubmitted("order-123", "customer-42", 42.50m),
    new PublishOptions
    {
        Key = "order-123",
        MessageId = "order-123"
    },
    cancellationToken);
```

Handle a consumed message:

```csharp
public sealed class OrderSubmittedHandler : IMessageHandler<OrderSubmitted>
{
    public Task HandleAsync(
        MessageContext context,
        OrderSubmitted message,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

## Next Step

Refine the public API surface and broaden the documentation as the library hardens.
