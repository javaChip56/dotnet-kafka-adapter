namespace DotNetKafkaAdapter.Abstractions;

public sealed record MessageContext(
    string Topic,
    string? Key = null,
    string? MessageId = null,
    IReadOnlyDictionary<string, string?>? Headers = null,
    long? Partition = null,
    long? Offset = null,
    DateTimeOffset? Timestamp = null);
