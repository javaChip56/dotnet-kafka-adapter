using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using DotNetKafkaAdapter.Abstractions;
using DotNetKafkaAdapter.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotNetKafkaAdapter.Consuming;

public sealed class KafkaConsumerHostedService : BackgroundService
{
    private readonly KafkaAdapterOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JsonSerializerOptions _serializerOptions;

    public KafkaConsumerHostedService(
        KafkaAdapterOptions options,
        IServiceScopeFactory scopeFactory,
        JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(scopeFactory);

        _options = options;
        _scopeFactory = scopeFactory;
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Consumers.Count == 0)
        {
            return Task.CompletedTask;
        }

        var consumerTasks = _options.Consumers
            .Select(registration => RunConsumerLoopAsync(registration, stoppingToken))
            .ToArray();

        return Task.WhenAll(consumerTasks);
    }

    private async Task RunConsumerLoopAsync(
        KafkaConsumerRegistration registration,
        CancellationToken stoppingToken)
    {
        ValidateRegistration(registration);

        var handlerInvoker = KafkaMessageHandlerInvoker.Create(registration);

        using var consumer = new ConsumerBuilder<string?, string>(KafkaConsumerConfigFactory.Create(_options, registration))
            .Build();

        consumer.Subscribe(registration.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);
                var payload = consumeResult.Message.Value;
                if (payload is null)
                {
                    throw new InvalidOperationException(
                        $"Kafka message on topic '{registration.Topic}' had a null payload, which is not supported by this adapter.");
                }

                var message = JsonSerializer.Deserialize(payload, registration.MessageType, _serializerOptions);
                if (message is null)
                {
                    throw new InvalidOperationException(
                        $"Kafka message on topic '{registration.Topic}' could not be deserialized into '{registration.MessageType.FullName}'.");
                }

                using var scope = _scopeFactory.CreateScope();
                var context = CreateMessageContext(consumeResult);

                await handlerInvoker
                    .InvokeAsync(scope.ServiceProvider, registration.HandlerType, context, message, stoppingToken)
                    .ConfigureAwait(false);

                if (!registration.AutoCommit)
                {
                    consumer.Commit(consumeResult);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            consumer.Close();
        }
    }

    private static MessageContext CreateMessageContext(ConsumeResult<string?, string> consumeResult)
    {
        var headers = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? messageId = null;

        foreach (var header in consumeResult.Message.Headers)
        {
            var headerValue = header.GetValueBytes() is { Length: > 0 } valueBytes
                ? Encoding.UTF8.GetString(valueBytes)
                : null;

            headers[header.Key] = headerValue;

            if (string.Equals(header.Key, KafkaMessageHeaders.MessageId, StringComparison.OrdinalIgnoreCase))
            {
                messageId = headerValue;
            }
        }

        return new MessageContext(
            consumeResult.Topic,
            consumeResult.Message.Key,
            messageId,
            headers,
            consumeResult.Partition.Value,
            consumeResult.Offset.Value,
            consumeResult.Message.Timestamp.UtcDateTime == default
                ? null
                : new DateTimeOffset(consumeResult.Message.Timestamp.UtcDateTime));
    }

    private static void ValidateRegistration(KafkaConsumerRegistration registration)
    {
        if (string.IsNullOrWhiteSpace(registration.Topic))
        {
            throw new InvalidOperationException("Kafka consumer topic must be configured.");
        }

        if (registration.MessageType == typeof(object))
        {
            throw new InvalidOperationException("Kafka consumer message type must be configured.");
        }

        if (registration.HandlerType == typeof(object))
        {
            throw new InvalidOperationException("Kafka consumer handler type must be configured.");
        }
    }
}
