namespace DotNetKafkaAdapter.Configuration;

public sealed class KafkaConsumerRegistration
{
    public string Topic { get; set; } = string.Empty;

    public string ConsumerGroup { get; set; } = string.Empty;

    public Type MessageType { get; internal set; } = typeof(object);

    public Type HandlerType { get; internal set; } = typeof(object);

    public ConsumerOffsetResetStrategy OffsetReset { get; set; } = ConsumerOffsetResetStrategy.Earliest;

    public bool AutoCommit { get; set; }

    public int MaxRetryAttempts { get; set; } = 3;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    public string? DeadLetterTopic { get; set; }

    internal static KafkaConsumerRegistration Create<TMessage, THandler>(
        string topic,
        string consumerGroup,
        Action<KafkaHandlerOptions>? configure = null)
    {
        var options = new KafkaHandlerOptions();
        configure?.Invoke(options);

        return new KafkaConsumerRegistration
        {
            Topic = topic,
            ConsumerGroup = consumerGroup,
            MessageType = typeof(TMessage),
            HandlerType = typeof(THandler),
            OffsetReset = options.OffsetReset,
            AutoCommit = options.AutoCommit,
            MaxRetryAttempts = options.MaxRetryAttempts,
            RetryDelay = options.RetryDelay,
            DeadLetterTopic = options.DeadLetterTopic
        };
    }

    internal KafkaConsumerRegistration Clone()
    {
        return new KafkaConsumerRegistration
        {
            Topic = Topic,
            ConsumerGroup = ConsumerGroup,
            MessageType = MessageType,
            HandlerType = HandlerType,
            OffsetReset = OffsetReset,
            AutoCommit = AutoCommit,
            MaxRetryAttempts = MaxRetryAttempts,
            RetryDelay = RetryDelay,
            DeadLetterTopic = DeadLetterTopic
        };
    }
}
