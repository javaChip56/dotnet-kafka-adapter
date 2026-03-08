namespace DotNetKafkaAdapter.Abstractions;

public interface IMessageHandler<in TMessage>
{
    Task HandleAsync(
        MessageContext context,
        TMessage message,
        CancellationToken cancellationToken = default);
}
