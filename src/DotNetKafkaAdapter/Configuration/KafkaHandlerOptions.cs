namespace DotNetKafkaAdapter.Configuration;

public sealed class KafkaHandlerOptions
{
    public ConsumerOffsetResetStrategy OffsetReset { get; set; } = ConsumerOffsetResetStrategy.Earliest;

    public bool AutoCommit { get; set; }

    public int MaxRetryAttempts { get; set; } = 3;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    public string? DeadLetterTopic { get; set; }
}
