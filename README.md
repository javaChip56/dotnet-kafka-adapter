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

This repository is at the project bootstrap stage. No implementation exists yet.

## Checklist

### Done

- [x] Confirmed the library is technically feasible as a lightweight .NET adapter over Kafka
- [x] Defined the high-level goal and initial project scope
- [x] Created the initial project README

### To Do

- [ ] Create the .NET solution and class library structure
- [ ] Choose target framework(s)
- [ ] Add Kafka client dependency (`Confluent.Kafka`)
- [ ] Define core abstractions for publishing and consuming
- [ ] Define configuration model for brokers, topics, auth, and consumer groups
- [ ] Implement producer wrapper for typed messages
- [ ] Implement consumer worker pattern for typed handlers
- [ ] Add JSON serializer/deserializer support
- [ ] Add DI registration extensions
- [ ] Add structured logging around produce/consume failures
- [ ] Decide offset commit strategy and failure semantics
- [ ] Add retry behavior and dead-letter strategy
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

Create the solution structure and define the public interfaces before writing Kafka-specific implementation code.
