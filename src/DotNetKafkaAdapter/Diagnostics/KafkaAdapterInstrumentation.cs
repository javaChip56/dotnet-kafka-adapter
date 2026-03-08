namespace DotNetKafkaAdapter.Diagnostics;

/// <summary>
/// Provides stable metric and instrument names for the adapter's built-in telemetry.
/// </summary>
public static class KafkaAdapterInstrumentation
{
    /// <summary>
    /// The <see cref="System.Diagnostics.Metrics.Meter"/> name emitted by the adapter.
    /// </summary>
    public const string MeterName = "DotNetKafkaAdapter";

    /// <summary>
    /// Counter for successfully published messages.
    /// </summary>
    public const string MessagesPublished = "dotnet_kafka_adapter.messages.published";

    /// <summary>
    /// Counter for publish failures.
    /// </summary>
    public const string PublishFailures = "dotnet_kafka_adapter.publish.failures";

    /// <summary>
    /// Histogram for publish duration in milliseconds.
    /// </summary>
    public const string PublishDuration = "dotnet_kafka_adapter.publish.duration";

    /// <summary>
    /// Counter for messages successfully handled by consumers.
    /// </summary>
    public const string MessagesHandled = "dotnet_kafka_adapter.messages.handled";

    /// <summary>
    /// Counter for consumer polling failures.
    /// </summary>
    public const string ConsumeFailures = "dotnet_kafka_adapter.consume.failures";

    /// <summary>
    /// Counter for deserialization failures.
    /// </summary>
    public const string DeserializationFailures = "dotnet_kafka_adapter.deserialization.failures";

    /// <summary>
    /// Counter for handler failures after all retries are exhausted.
    /// </summary>
    public const string HandlerFailures = "dotnet_kafka_adapter.handler.failures";

    /// <summary>
    /// Counter for in-process retry attempts.
    /// </summary>
    public const string RetryAttempts = "dotnet_kafka_adapter.retry.attempts";

    /// <summary>
    /// Counter for messages published to a dead-letter topic.
    /// </summary>
    public const string DeadLetterMessages = "dotnet_kafka_adapter.dead_letter.published";

    /// <summary>
    /// Counter for offset commit failures.
    /// </summary>
    public const string OffsetCommitFailures = "dotnet_kafka_adapter.offset_commit.failures";

    /// <summary>
    /// Up-down counter for currently active consumer loops.
    /// </summary>
    public const string ActiveConsumers = "dotnet_kafka_adapter.consumers.active";
}
