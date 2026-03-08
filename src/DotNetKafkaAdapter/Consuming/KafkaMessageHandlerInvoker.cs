using DotNetKafkaAdapter.Abstractions;
using DotNetKafkaAdapter.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetKafkaAdapter.Consuming;

internal abstract class KafkaMessageHandlerInvoker
{
    public static KafkaMessageHandlerInvoker Create(KafkaConsumerRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var handlerContract = typeof(IMessageHandler<>).MakeGenericType(registration.MessageType);
        if (!handlerContract.IsAssignableFrom(registration.HandlerType))
        {
            throw new InvalidOperationException(
                $"Handler type '{registration.HandlerType.FullName}' must implement '{handlerContract.FullName}'.");
        }

        var invokerType = typeof(KafkaMessageHandlerInvoker<>).MakeGenericType(registration.MessageType);
        return (KafkaMessageHandlerInvoker)Activator.CreateInstance(invokerType)!;
    }

    public abstract Task InvokeAsync(
        IServiceProvider serviceProvider,
        Type handlerType,
        MessageContext context,
        object message,
        CancellationToken cancellationToken);
}

internal sealed class KafkaMessageHandlerInvoker<TMessage> : KafkaMessageHandlerInvoker
{
    public override Task InvokeAsync(
        IServiceProvider serviceProvider,
        Type handlerType,
        MessageContext context,
        object message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        var handler = serviceProvider.GetRequiredService(handlerType) as IMessageHandler<TMessage>;
        if (handler is null)
        {
            throw new InvalidOperationException(
                $"Registered handler '{handlerType.FullName}' could not be resolved as IMessageHandler<{typeof(TMessage).FullName}>.");
        }

        return handler.HandleAsync(context, (TMessage)message, cancellationToken);
    }
}
