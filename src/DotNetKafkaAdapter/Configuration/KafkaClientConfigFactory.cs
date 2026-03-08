using Confluent.Kafka;

namespace DotNetKafkaAdapter.Configuration;

internal static class KafkaClientConfigFactory
{
    public static void ApplySecurity(ClientConfig config, KafkaSecurityOptions security)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(security);

        config.SecurityProtocol = security.Protocol switch
        {
            KafkaSecurityProtocol.Plaintext => SecurityProtocol.Plaintext,
            KafkaSecurityProtocol.Ssl => SecurityProtocol.Ssl,
            KafkaSecurityProtocol.SaslPlaintext => SecurityProtocol.SaslPlaintext,
            KafkaSecurityProtocol.SaslSsl => SecurityProtocol.SaslSsl,
            _ => throw new ArgumentOutOfRangeException(nameof(security.Protocol), security.Protocol, null)
        };

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
}
