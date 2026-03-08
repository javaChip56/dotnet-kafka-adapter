namespace DotNetKafkaAdapter.Consuming;

public sealed record KafkaDeadLetterMessage(
    string OriginalTopic,
    string ConsumerGroup,
    string? Key,
    string? MessageId,
    string Payload,
    IReadOnlyDictionary<string, string?> Headers,
    long? Partition,
    long? Offset,
    DateTimeOffset? Timestamp,
    string MessageType,
    string HandlerType,
    string FailureStage,
    string ErrorMessage,
    string? ExceptionType,
    int Attempts,
    DateTimeOffset FailedAtUtc);
