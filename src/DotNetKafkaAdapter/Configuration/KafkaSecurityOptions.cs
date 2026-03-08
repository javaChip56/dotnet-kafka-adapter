namespace DotNetKafkaAdapter.Configuration;

public sealed class KafkaSecurityOptions
{
    public KafkaSecurityProtocol Protocol { get; set; } = KafkaSecurityProtocol.Plaintext;

    public KafkaSaslMechanism SaslMechanism { get; set; } = KafkaSaslMechanism.Plain;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? SslCaLocation { get; set; }

    public string? SslCaPem { get; set; }

    public string? SslCaCertificateStores { get; set; }

    public string? SslCertificateLocation { get; set; }

    public string? SslCertificatePem { get; set; }

    public string? SslKeyLocation { get; set; }

    public string? SslKeyPem { get; set; }

    public string? SslKeyPassword { get; set; }

    public string? SslKeystoreLocation { get; set; }

    public string? SslKeystorePassword { get; set; }

    public bool? EnableSslCertificateVerification { get; set; }

    public KafkaSslEndpointIdentificationAlgorithm? SslEndpointIdentificationAlgorithm { get; set; }
}
