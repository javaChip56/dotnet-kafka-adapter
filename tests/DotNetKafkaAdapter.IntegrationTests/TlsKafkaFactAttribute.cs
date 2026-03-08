using Xunit;

namespace DotNetKafkaAdapter.IntegrationTests;

public sealed class TlsKafkaFactAttribute : FactAttribute
{
    public TlsKafkaFactAttribute()
    {
        var certDirectory = KafkaTestAssetPaths.GetTlsCertificateDirectory();

        var requiredFiles = new[]
        {
            Path.Combine(certDirectory, "ca.pem"),
            Path.Combine(certDirectory, "client.crt"),
            Path.Combine(certDirectory, "client.key")
        };

        if (requiredFiles.Any(file => !File.Exists(file)))
        {
            Skip = $"Generate TLS assets first with scripts/generate-kafka-tls-certs.ps1. Missing assets under {certDirectory}.";
        }
    }
}
