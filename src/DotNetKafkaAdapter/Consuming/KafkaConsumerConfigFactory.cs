using Confluent.Kafka;
using DotNetKafkaAdapter.Configuration;

namespace DotNetKafkaAdapter.Consuming;

internal static class KafkaConsumerConfigFactory
{
    public static ConsumerConfig Create(
        KafkaAdapterOptions options,
        KafkaConsumerRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(registration);

        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers must be configured.");
        }

        if (string.IsNullOrWhiteSpace(registration.ConsumerGroup))
        {
            throw new InvalidOperationException("Kafka consumer group must be configured.");
        }

        return new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
            GroupId = registration.ConsumerGroup,
            EnableAutoCommit = registration.AutoCommit,
            AutoOffsetReset = registration.OffsetReset switch
            {
                ConsumerOffsetResetStrategy.Earliest => AutoOffsetReset.Earliest,
                ConsumerOffsetResetStrategy.Latest => AutoOffsetReset.Latest,
                _ => throw new ArgumentOutOfRangeException(nameof(registration.OffsetReset), registration.OffsetReset, null)
            }
        }.ApplySecurity(options.Security);
    }

    private static ConsumerConfig ApplySecurity(
        this ConsumerConfig config,
        KafkaSecurityOptions security)
    {
        KafkaClientConfigFactory.ApplySecurity(config, security);
        return config;
    }
}
