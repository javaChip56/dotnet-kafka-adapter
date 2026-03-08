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

        services.AddOptions<KafkaAdapterOptions>().Configure(configure);
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<KafkaAdapterOptions>>().Value);

        return services.AddKafkaAdapterServices();
    }

    public static IServiceCollection AddKafkaAdapter(
        this IServiceCollection services,
        KafkaAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddOptions();
        services.TryAddSingleton(options);
        services.TryAddSingleton<IOptions<KafkaAdapterOptions>>(_ => new OptionsWrapper<KafkaAdapterOptions>(options));

        return services.AddKafkaAdapterServices();
    }

    private static IServiceCollection AddKafkaAdapterServices(this IServiceCollection services)
    {
        services.TryAddSingleton<KafkaMessagePublisher>(sp =>
        {
            var options = sp.GetRequiredService<KafkaAdapterOptions>();
            var serializerOptions = sp.GetService<JsonSerializerOptions>();

            return new KafkaMessagePublisher(options, serializerOptions);
        });

        services.TryAddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<KafkaMessagePublisher>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, KafkaConsumerHostedService>());

        return services;
    }
}
