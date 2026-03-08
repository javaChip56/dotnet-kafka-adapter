using Confluent.Kafka;

namespace DotNetKafkaAdapter.Configuration;

internal static class KafkaClientConfigFactory
{
    public static void ApplySecurity(ClientConfig config, KafkaSecurityOptions security)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(security);

        var securityProtocol = security.Protocol switch
        {
            KafkaSecurityProtocol.Plaintext => SecurityProtocol.Plaintext,
            KafkaSecurityProtocol.Ssl => SecurityProtocol.Ssl,
            KafkaSecurityProtocol.SaslPlaintext => SecurityProtocol.SaslPlaintext,
            KafkaSecurityProtocol.SaslSsl => SecurityProtocol.SaslSsl,
            _ => throw new ArgumentOutOfRangeException(nameof(security.Protocol), security.Protocol, null)
        };

        config.SecurityProtocol = securityProtocol;

        if (securityProtocol is SecurityProtocol.Ssl or SecurityProtocol.SaslSsl)
        {
            ApplySsl(config, security);
        }
        else
        {
            ClearSsl(config);
        }

        if (securityProtocol is not SecurityProtocol.SaslPlaintext and not SecurityProtocol.SaslSsl)
        {
            config.SaslMechanism = null;
            config.SaslUsername = null;
            config.SaslPassword = null;
            return;
        }

        config.SaslMechanism = security.SaslMechanism switch
        {
            KafkaSaslMechanism.Plain => SaslMechanism.Plain,
            KafkaSaslMechanism.ScramSha256 => SaslMechanism.ScramSha256,
            KafkaSaslMechanism.ScramSha512 => SaslMechanism.ScramSha512,
            _ => throw new ArgumentOutOfRangeException(nameof(security.SaslMechanism), security.SaslMechanism, null)
        };

        config.SaslUsername = security.Username;
        config.SaslPassword = security.Password;
    }

    private static void ApplySsl(ClientConfig config, KafkaSecurityOptions security)
    {
        config.SslCaLocation = security.SslCaLocation;
        config.SslCaPem = security.SslCaPem;
        config.SslCaCertificateStores = security.SslCaCertificateStores;
        config.SslCertificateLocation = security.SslCertificateLocation;
        config.SslCertificatePem = security.SslCertificatePem;
        config.SslKeyLocation = security.SslKeyLocation;
        config.SslKeyPem = security.SslKeyPem;
        config.SslKeyPassword = security.SslKeyPassword;
        config.SslKeystoreLocation = security.SslKeystoreLocation;
        config.SslKeystorePassword = security.SslKeystorePassword;

        if (security.EnableSslCertificateVerification.HasValue)
        {
            config.EnableSslCertificateVerification = security.EnableSslCertificateVerification.Value;
        }

        if (security.SslEndpointIdentificationAlgorithm.HasValue)
        {
            config.SslEndpointIdentificationAlgorithm = security.SslEndpointIdentificationAlgorithm.Value switch
            {
                KafkaSslEndpointIdentificationAlgorithm.Https => SslEndpointIdentificationAlgorithm.Https,
                KafkaSslEndpointIdentificationAlgorithm.None => SslEndpointIdentificationAlgorithm.None,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(security.SslEndpointIdentificationAlgorithm),
                    security.SslEndpointIdentificationAlgorithm,
                    null)
            };
        }
    }

    private static void ClearSsl(ClientConfig config)
    {
        config.SslCaLocation = null;
        config.SslCaPem = null;
        config.SslCaCertificateStores = null;
        config.SslCertificateLocation = null;
        config.SslCertificatePem = null;
        config.SslKeyLocation = null;
        config.SslKeyPem = null;
        config.SslKeyPassword = null;
        config.SslKeystoreLocation = null;
        config.SslKeystorePassword = null;
        config.EnableSslCertificateVerification = null;
        config.SslEndpointIdentificationAlgorithm = null;
    }
}
