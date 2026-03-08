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

This repository now contains the initial solution scaffold, public contracts, producer and consumer implementations, DI registration, typed handler registration helpers, and first-pass retry/dead-letter behavior.

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

### To Do

- [ ] Add local development setup for Kafka
- [ ] Add integration tests against a local Kafka instance
- [ ] Add sample application showing publish/consume usage
- [ ] Add usage documentation and configuration examples

## Proposed Deliverables

The likely shape of the library:

- A reusable adapter package
- A small sample app
- Integration tests
- Documentation for setup and usage

## Next Step

Add local Kafka development setup, integration tests, and usage examples.
