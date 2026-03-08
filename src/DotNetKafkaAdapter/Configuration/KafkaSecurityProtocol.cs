namespace DotNetKafkaAdapter.Configuration;

public enum KafkaSecurityProtocol
{
    Plaintext = 0,
    Ssl = 1,
    SaslPlaintext = 2,
    SaslSsl = 3
}
