namespace DotNetKafkaAdapter.Configuration;

public sealed class KafkaConsumerRegistration
{
    public string Topic { get; set; } = string.Empty;

    public string ConsumerGroup { get; set; } = string.Empty;

    public Type MessageType { get; set; } = typeof(object);

    public Type HandlerType { get; set; } = typeof(object);

    public ConsumerOffsetResetStrategy OffsetReset { get; set; } = ConsumerOffsetResetStrategy.Earliest;

    public bool AutoCommit { get; set; }
}
