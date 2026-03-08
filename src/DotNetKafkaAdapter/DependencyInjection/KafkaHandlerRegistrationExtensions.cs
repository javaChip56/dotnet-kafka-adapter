using DotNetKafkaAdapter.Abstractions;
using DotNetKafkaAdapter.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class KafkaHandlerRegistrationExtensions
{
    public static IServiceCollection AddKafkaHandler<TMessage, THandler>(
        this IServiceCollection services,
        string topic,
        string consumerGroup,
        Action<KafkaHandlerOptions>? configure = null)
        where THandler : class, IMessageHandler<TMessage>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);

        services.AddOptions<KafkaAdapterOptions>();
        services.TryAddScoped<THandler>();
        services.Configure<KafkaAdapterOptions>(options => options.AddConsumer<TMessage, THandler>(topic, consumerGroup, configure));

        return services;
    }
}
