namespace DotNetKafkaAdapter.IntegrationTests;

internal static class KafkaTestAssetPaths
{
    public static string GetTlsCertificateDirectory()
        => GetRepoRelativePath("dev", "kafka-tls", "certs");

    private static string GetRepoRelativePath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DotNetKafkaAdapter.sln")))
            {
                return Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root for integration test assets.");
    }
}
