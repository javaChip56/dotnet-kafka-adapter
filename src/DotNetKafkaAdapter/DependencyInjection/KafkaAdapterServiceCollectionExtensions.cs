using System.Text.Json;
using DotNetKafkaAdapter.Abstractions;
using DotNetKafkaAdapter.Consuming;
using DotNetKafkaAdapter.Configuration;
using DotNetKafkaAdapter.Producing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class KafkaAdapterServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaAdapter(
        this IServiceCollection services,
        Action<KafkaAdapterOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.EnsureKafkaOptions();
        services.Configure(configure);

        return services.AddKafkaAdapterServices();
    }

    public static IServiceCollection AddKafkaAdapter(
        this IServiceCollection services,
        KafkaAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.EnsureKafkaOptions();
        services.Configure<KafkaAdapterOptions>(target => ApplyOptions(target, options));

        return services.AddKafkaAdapterServices();
    }

    private static IServiceCollection AddKafkaAdapterServices(this IServiceCollection services)
    {
        services.TryAddSingleton<KafkaMessagePublisher>(sp =>
        {
            var options = sp.GetRequiredService<KafkaAdapterOptions>();
            var serializerOptions = sp.GetService<JsonSerializerOptions>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<KafkaMessagePublisher>>();

            return new KafkaMessagePublisher(options, serializerOptions, logger);
        });

        services.TryAddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<KafkaMessagePublisher>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, KafkaConsumerHostedService>());

        return services;
    }

    private static IServiceCollection EnsureKafkaOptions(this IServiceCollection services)
    {
        services.AddOptions<KafkaAdapterOptions>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<KafkaAdapterOptions>>().Value);

        return services;
    }

    private static void ApplyOptions(KafkaAdapterOptions target, KafkaAdapterOptions source)
    {
        target.BootstrapServers = source.BootstrapServers;
        target.ClientId = source.ClientId;
        target.Security = new KafkaSecurityOptions
        {
            Protocol = source.Security.Protocol,
            SaslMechanism = source.Security.SaslMechanism,
            Username = source.Security.Username,
            Password = source.Security.Password
        };
        target.Producer = new KafkaProducerOptions
        {
            DefaultTopic = source.Producer.DefaultTopic,
            EnableIdempotence = source.Producer.EnableIdempotence
        };

        foreach (var registration in source.Consumers)
        {
            target.Consumers.Add(new KafkaConsumerRegistration
            {
                Topic = registration.Topic,
                ConsumerGroup = registration.ConsumerGroup,
                MessageType = registration.MessageType,
                HandlerType = registration.HandlerType,
                OffsetReset = registration.OffsetReset,
                AutoCommit = registration.AutoCommit,
                MaxRetryAttempts = registration.MaxRetryAttempts,
                RetryDelay = registration.RetryDelay,
                DeadLetterTopic = registration.DeadLetterTopic
            });
        }
    }
}
