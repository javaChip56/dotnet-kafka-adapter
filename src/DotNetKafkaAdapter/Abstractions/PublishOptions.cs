namespace DotNetKafkaAdapter.Abstractions;

public sealed record PublishOptions
{
    public string? Key { get; init; }

    public string? MessageId { get; init; }

    public IReadOnlyDictionary<string, string?>? Headers { get; init; }
}
