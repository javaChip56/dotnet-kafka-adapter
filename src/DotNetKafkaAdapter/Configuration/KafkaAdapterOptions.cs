using DotNetKafkaAdapter.Abstractions;

namespace DotNetKafkaAdapter.Configuration;

public sealed class KafkaAdapterOptions
{
    public string BootstrapServers { get; set; } = string.Empty;

    public string? ClientId { get; set; }

    public KafkaSecurityOptions Security { get; set; } = new();

    public KafkaProducerOptions Producer { get; set; } = new();

    public List<KafkaConsumerRegistration> Consumers { get; set; } = [];

    public KafkaConsumerRegistration AddConsumer<TMessage, THandler>(
        string topic,
        string consumerGroup,
        Action<KafkaHandlerOptions>? configure = null)
        where THandler : class, IMessageHandler<TMessage>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);

        var registration = KafkaConsumerRegistration.Create<TMessage, THandler>(
            topic,
            consumerGroup,
            configure);

        Consumers.Add(registration);

        return registration;
    }
}
