using Confluent.Kafka;
using DotNetKafkaAdapter.Configuration;

namespace DotNetKafkaAdapter.Producing;

internal static class KafkaProducerConfigFactory
{
    public static ProducerConfig Create(KafkaAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers must be configured.");
        }

        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
            EnableIdempotence = options.Producer.EnableIdempotence
        };

        KafkaClientConfigFactory.ApplySecurity(config, options.Security);

        return config;
    }
}
