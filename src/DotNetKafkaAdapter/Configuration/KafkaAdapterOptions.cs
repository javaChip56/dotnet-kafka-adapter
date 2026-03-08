namespace DotNetKafkaAdapter.Configuration;

public sealed class KafkaAdapterOptions
{
    public string BootstrapServers { get; set; } = string.Empty;

    public string? ClientId { get; set; }

    public KafkaSecurityOptions Security { get; set; } = new();

    public KafkaProducerOptions Producer { get; set; } = new();

    public List<KafkaConsumerRegistration> Consumers { get; set; } = [];
}
