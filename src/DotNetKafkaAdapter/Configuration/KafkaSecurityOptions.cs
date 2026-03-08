namespace DotNetKafkaAdapter.Configuration;

public sealed class KafkaSecurityOptions
{
    public KafkaSecurityProtocol Protocol { get; set; } = KafkaSecurityProtocol.Plaintext;

    public KafkaSaslMechanism SaslMechanism { get; set; } = KafkaSaslMechanism.Plain;

    public string? Username { get; set; }

    public string? Password { get; set; }
}
