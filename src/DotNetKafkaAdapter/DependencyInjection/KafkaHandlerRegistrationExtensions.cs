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
        Action<KafkaConsumerRegistration>? configure = null)
        where THandler : class, IMessageHandler<TMessage>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerGroup);

        services.AddOptions<KafkaAdapterOptions>();
        services.TryAddScoped<THandler>();
        services.Configure<KafkaAdapterOptions>(options =>
        {
            var registration = new KafkaConsumerRegistration
            {
                Topic = topic,
                ConsumerGroup = consumerGroup,
                MessageType = typeof(TMessage),
                HandlerType = typeof(THandler)
            };

            configure?.Invoke(registration);
            options.Consumers.Add(registration);
        });

        return services;
    }
}
