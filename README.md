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
- [x] Narrowed the handler registration API to consumer-facing options
- [x] Added production-oriented failure and authentication guidance
- [x] Added optional certificate-based TLS broker security settings
- [x] Added a TLS-enabled local Kafka integration test path
- [x] Added initial NuGet package metadata and pack/push instructions
- [x] Refined public-release package metadata
- [x] Added operational guidance for monitoring, replay, and dead-letter reprocessing

### To Do

- [ ] Decide whether to add a license file and explicit NuGet license metadata before public publication

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

To run the TLS integration test as well:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/generate-kafka-tls-certs.ps1
docker compose -f docker-compose.kafka.tls.yml up -d
dotnet test tests/DotNetKafkaAdapter.IntegrationTests/DotNetKafkaAdapter.IntegrationTests.csproj
```

The TLS broker listens on `localhost:9093` and requires:

- server certificate validation against the generated local CA
- client certificate authentication with the generated client certificate and private key

The TLS-specific test is skipped automatically if the generated certificate assets are not present.

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

## NuGet Packaging

The adapter library can be packed from [src/DotNetKafkaAdapter](D:\Research\dotnet-kafka-adapter\src\DotNetKafkaAdapter).

The project currently packs with:

- package ID `DotNetKafkaAdapter`
- default version `0.1.0`
- the repository `README.md` included as the NuGet package readme

Create a package locally with:

```bash
dotnet pack src/DotNetKafkaAdapter/DotNetKafkaAdapter.csproj -c Release -o artifacts/packages
```

To set an explicit package version at pack time:

```bash
dotnet pack src/DotNetKafkaAdapter/DotNetKafkaAdapter.csproj -c Release -o artifacts/packages /p:Version=0.1.0
```

The generated `.nupkg` file will be written to `artifacts/packages`.

The package metadata is defined in [DotNetKafkaAdapter.csproj](D:\Research\dotnet-kafka-adapter\src\DotNetKafkaAdapter\DotNetKafkaAdapter.csproj). If you are preparing a public release, you will likely still want to set or refine:

- `Version`
- `Authors`
- license metadata appropriate for your release target

Push a built package with:

```bash
dotnet nuget push artifacts/packages/DotNetKafkaAdapter.<version>.nupkg --source https://api.nuget.org/v3/index.json --api-key <your-api-key>
```

If you want symbol packages as well, include them when packing:

```bash
dotnet pack src/DotNetKafkaAdapter/DotNetKafkaAdapter.csproj -c Release -o artifacts/packages /p:IncludeSymbols=true /p:SymbolPackageFormat=snupkg
```

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
    options =>
    {
        options.MaxRetryAttempts = 3;
        options.RetryDelay = TimeSpan.FromSeconds(1);
        options.DeadLetterTopic = "orders.dlq";
    });
```

If you prefer to build options up front instead of registering handlers through DI extensions:

```csharp
var kafkaOptions = new KafkaAdapterOptions
{
    BootstrapServers = "localhost:9092",
    ClientId = "my-app"
};

kafkaOptions.Producer.DefaultTopic = "orders";
kafkaOptions.AddConsumer<OrderSubmitted, OrderSubmittedHandler>(
    topic: "orders",
    consumerGroup: "orders-consumer",
    configure: options =>
    {
        options.MaxRetryAttempts = 3;
        options.RetryDelay = TimeSpan.FromSeconds(1);
        options.DeadLetterTopic = "orders.dlq";
    });

services.AddKafkaAdapter(kafkaOptions);
services.AddScoped<OrderSubmittedHandler>();
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

## Failure Semantics

Current consumer behavior:

- If a handler succeeds and `AutoCommit` is `false`, the adapter commits the Kafka offset after handling.
- If deserialization fails, the adapter does not retry in-process. It sends the message to the dead-letter topic if one is configured; otherwise that consumer loop stops.
- If the handler throws, the adapter retries in-process up to `MaxRetryAttempts` with exponential backoff based on `RetryDelay`.
- If all retries fail and a dead-letter topic is configured, the adapter publishes a `KafkaDeadLetterMessage` and then commits the original offset.
- If all retries fail and no dead-letter topic is configured, that consumer loop stops without committing the failed message.

What this does not guarantee:

- Exactly-once delivery
- Global ordering beyond Kafka partition semantics
- Distributed retries across processes
- Automatic replay or re-drive of dead-lettered messages

## Authentication

Local development usually uses plaintext:

```csharp
services.AddKafkaAdapter(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.ClientId = "my-app";
});
```

For SASL/PLAIN over TLS:

```csharp
services.AddKafkaAdapter(options =>
{
    options.BootstrapServers = "your-broker:9093";
    options.ClientId = "my-app";
    options.Security.Protocol = KafkaSecurityProtocol.SaslSsl;
    options.Security.SaslMechanism = KafkaSaslMechanism.Plain;
    options.Security.Username = "my-username";
    options.Security.Password = "my-password";
});
```

For SASL/SCRAM over TLS:

```csharp
services.AddKafkaAdapter(options =>
{
    options.BootstrapServers = "your-broker:9093";
    options.ClientId = "my-app";
    options.Security.Protocol = KafkaSecurityProtocol.SaslSsl;
    options.Security.SaslMechanism = KafkaSaslMechanism.ScramSha512;
    options.Security.Username = "my-username";
    options.Security.Password = "my-password";
});
```

For TLS server verification with a custom CA bundle:

```csharp
services.AddKafkaAdapter(options =>
{
    options.BootstrapServers = "your-broker:9093";
    options.ClientId = "my-app";
    options.Security.Protocol = KafkaSecurityProtocol.Ssl;
    options.Security.SslCaLocation = "/etc/certs/ca.pem";
    options.Security.EnableSslCertificateVerification = true;
    options.Security.SslEndpointIdentificationAlgorithm = KafkaSslEndpointIdentificationAlgorithm.Https;
});
```

For mutual TLS with a client certificate and private key:

```csharp
services.AddKafkaAdapter(options =>
{
    options.BootstrapServers = "your-broker:9093";
    options.ClientId = "my-app";
    options.Security.Protocol = KafkaSecurityProtocol.Ssl;
    options.Security.SslCaLocation = "/etc/certs/ca.pem";
    options.Security.SslCertificateLocation = "/etc/certs/client.pem";
    options.Security.SslKeyLocation = "/etc/certs/client.key";
    options.Security.SslKeyPassword = "optional-key-password";
});
```

Optional TLS settings currently supported:

- `SslCaLocation`
- `SslCaPem`
- `SslCaCertificateStores`
- `SslCertificateLocation`
- `SslCertificatePem`
- `SslKeyLocation`
- `SslKeyPem`
- `SslKeyPassword`
- `SslKeystoreLocation`
- `SslKeystorePassword`
- `EnableSslCertificateVerification`
- `SslEndpointIdentificationAlgorithm`

## Production Notes

- Keep `BootstrapServers`, usernames, and passwords in configuration or secret storage rather than source code.
- Keep certificate file paths, PEM content, and key passwords in configuration or secret storage rather than source code.
- Use `KafkaSecurityProtocol.SaslSsl` or `KafkaSecurityProtocol.Ssl` for remote brokers unless you explicitly control a trusted plaintext network.
- Use the TLS settings only when your broker protocol is `Ssl` or `SaslSsl`; they remain optional and are ignored for plaintext configurations.
- Decide whether stopping a consumer loop on an unrecoverable failure is acceptable for your service model before using the current defaults in production.
- Provision dead-letter topics explicitly and monitor them; the adapter publishes DLQ messages but does not re-drive them.
- Integration tests in this repo validate the local broker path, not a production cluster topology.

## Monitoring And Operations

For production operation, monitor the adapter at both the service and Kafka level.

Recommended service-level signals:

- publish failures from `KafkaMessagePublisher`
- consumer loop failures from `KafkaConsumerHostedService`
- retry attempt counts
- dead-letter publish counts
- dead-letter topic growth over time
- handler processing latency
- consumer lag for each topic and consumer group

Recommended operational alerts:

- any consumer loop stops unexpectedly
- dead-letter traffic exceeds a normal baseline
- consumer lag continues to grow for a sustained window
- repeated deserialization failures for the same topic
- repeated handler failures for the same message type or handler

Operationally, the most important current distinction is:

- deserialization failures are not retried in-process
- handler failures are retried in-process and may be dead-lettered after retries are exhausted

That means deserialization failures usually point to a producer-contract mismatch, while handler failures usually point to business logic or downstream dependency issues.

## Dead-Letter Reprocessing

The adapter publishes a `KafkaDeadLetterMessage` payload to the configured dead-letter topic. The message includes:

- original topic
- consumer group
- original key and message ID
- serialized payload
- headers
- partition, offset, and timestamp
- message type and handler type
- failure stage, error message, exception type, and attempt count

Use that information to separate recovery paths:

- if `FailureStage` is `deserialization`, fix the producer or schema/contract mismatch before replaying
- if `FailureStage` is `handler`, fix the handler or downstream dependency before replaying
- if the payload is irrecoverable, archive it and do not replay it blindly

For replay, a safe default process is:

1. identify the root cause and deploy the fix first
2. inspect the DLQ payloads and filter to only the recoverable subset
3. republish those messages to the original topic or a dedicated retry topic
4. monitor consumer lag, retry counts, and DLQ volume during replay
5. keep replay idempotent at the handler level, because the adapter does not guarantee exactly-once handling

Avoid wiring automatic DLQ re-drive back into the same consumer path until you have explicit idempotency and replay controls in place. Otherwise a poison message can loop between the primary topic and DLQ.

## Replay Strategy

For low-volume recovery, manual replay with an operator-reviewed tool or script is the safest option.

For higher-volume recovery, prefer a separate replay worker that:

- reads from the DLQ topic
- validates message age, failure stage, and replay eligibility
- republishes at a controlled rate
- tags replayed messages with a header or message attribute if you add that behavior later
- writes an audit log of what was replayed and why

The current adapter intentionally does not include built-in replay automation. That keeps the core library small and avoids baking operational policy into the transport layer.

## Next Step

Decide how far to take production hardening, especially packaging and advanced broker security support.
