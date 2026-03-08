namespace DotNetKafkaAdapter.Configuration;

public sealed class KafkaProducerOptions
{
    public string? DefaultTopic { get; set; }

    public bool EnableIdempotence { get; set; } = true;
}
