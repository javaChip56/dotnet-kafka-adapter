using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using DotNetKafkaAdapter.Abstractions;
using DotNetKafkaAdapter.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetKafkaAdapter.Consuming;

public sealed class KafkaConsumerHostedService : BackgroundService
{
    private readonly KafkaAdapterOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<KafkaConsumerHostedService> _logger;

    public KafkaConsumerHostedService(
        KafkaAdapterOptions options,
        IServiceScopeFactory scopeFactory,
        IMessagePublisher messagePublisher,
        ILogger<KafkaConsumerHostedService> logger,
        JsonSerializerOptions? serializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(messagePublisher);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _scopeFactory = scopeFactory;
        _messagePublisher = messagePublisher;
        _logger = logger;
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Consumers.Count == 0)
        {
            _logger.LogDebug("Kafka consumer hosted service started with no consumer registrations.");
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
        _logger.LogInformation(
            "Starting Kafka consumer for topic {Topic} with consumer group {ConsumerGroup}.",
            registration.Topic,
            registration.ConsumerGroup);

        using var consumer = new ConsumerBuilder<string?, string>(KafkaConsumerConfigFactory.Create(_options, registration))
            .Build();

        consumer.Subscribe(registration.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string?, string> consumeResult;

                try
                {
                    consumeResult = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(
                        ex,
                        "Kafka consume failure for topic {Topic} and consumer group {ConsumerGroup}.",
                        registration.Topic,
                        registration.ConsumerGroup);

                    continue;
                }

                var shouldContinue = await ProcessMessageAsync(
                        consumer,
                        consumeResult,
                        registration,
                        handlerInvoker,
                        stoppingToken)
                    .ConfigureAwait(false);

                if (!shouldContinue)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Stopping Kafka consumer for topic {Topic} with consumer group {ConsumerGroup}.",
                registration.Topic,
                registration.ConsumerGroup);
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task<bool> ProcessMessageAsync(
        IConsumer<string?, string> consumer,
        ConsumeResult<string?, string> consumeResult,
        KafkaConsumerRegistration registration,
        KafkaMessageHandlerInvoker handlerInvoker,
        CancellationToken stoppingToken)
    {
        var context = CreateMessageContext(consumeResult);
        var payload = consumeResult.Message.Value;
        if (payload is null)
        {
            return await HandleTerminalFailureAsync(
                    consumer,
                    consumeResult,
                    registration,
                    context,
                    payload: string.Empty,
                    stage: "payload",
                    attempts: 1,
                    new InvalidOperationException(
                        $"Kafka message on topic '{registration.Topic}' had a null payload, which is not supported by this adapter."),
                    stoppingToken)
                .ConfigureAwait(false);
        }

        object? message;

        try
        {
            message = JsonSerializer.Deserialize(payload, registration.MessageType, _serializerOptions);
            if (message is null)
            {
                throw new InvalidOperationException(
                    $"Kafka message on topic '{registration.Topic}' could not be deserialized into '{registration.MessageType.FullName}'.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to deserialize Kafka message from topic {Topic} to message type {MessageType}.",
                registration.Topic,
                registration.MessageType.FullName);

            return await HandleTerminalFailureAsync(
                    consumer,
                    consumeResult,
                    registration,
                    context,
                    payload,
                    "deserialization",
                    attempts: 1,
                    ex,
                    stoppingToken)
                .ConfigureAwait(false);
        }

        for (var retryAttempt = 0; retryAttempt <= registration.MaxRetryAttempts; retryAttempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                await handlerInvoker
                    .InvokeAsync(scope.ServiceProvider, registration.HandlerType, context, message, stoppingToken)
                    .ConfigureAwait(false);

                if (!registration.AutoCommit)
                {
                    CommitConsumedMessage(consumer, consumeResult, registration);
                }

                return true;
            }
            catch (Exception ex) when (retryAttempt < registration.MaxRetryAttempts)
            {
                var delay = CalculateRetryDelay(registration.RetryDelay, retryAttempt + 1);

                _logger.LogWarning(
                    ex,
                    "Kafka handler {HandlerType} failed for topic {Topic}. Retrying attempt {RetryAttempt} of {MaxRetryAttempts} after {Delay}.",
                    registration.HandlerType.FullName,
                    registration.Topic,
                    retryAttempt + 1,
                    registration.MaxRetryAttempts,
                    delay);

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kafka handler {HandlerType} failed for topic {Topic} after {AttemptCount} attempts.",
                    registration.HandlerType.FullName,
                    registration.Topic,
                    retryAttempt + 1);

                return await HandleTerminalFailureAsync(
                        consumer,
                        consumeResult,
                        registration,
                        context,
                        payload,
                        "handler",
                        retryAttempt + 1,
                        ex,
                        stoppingToken)
                    .ConfigureAwait(false);
            }
        }

        return true;
    }

    private async Task<bool> HandleTerminalFailureAsync(
        IConsumer<string?, string> consumer,
        ConsumeResult<string?, string> consumeResult,
        KafkaConsumerRegistration registration,
        MessageContext context,
        string payload,
        string stage,
        int attempts,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(registration.DeadLetterTopic))
        {
            var deadLetterMessage = new KafkaDeadLetterMessage(
                context.Topic,
                registration.ConsumerGroup,
                context.Key,
                context.MessageId,
                payload,
                context.Headers ?? new Dictionary<string, string?>(),
                context.Partition,
                context.Offset,
                context.Timestamp,
                registration.MessageType.FullName ?? registration.MessageType.Name,
                registration.HandlerType.FullName ?? registration.HandlerType.Name,
                stage,
                exception.Message,
                exception.GetType().FullName,
                attempts,
                DateTimeOffset.UtcNow);

            _logger.LogWarning(
                exception,
                "Publishing Kafka message from topic {Topic} to dead-letter topic {DeadLetterTopic} after failure in stage {Stage}.",
                registration.Topic,
                registration.DeadLetterTopic,
                stage);

            await _messagePublisher
                .PublishAsync(
                    registration.DeadLetterTopic,
                    deadLetterMessage,
                    new PublishOptions
                    {
                        Key = context.Key,
                        MessageId = context.MessageId
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            CommitConsumedMessage(consumer, consumeResult, registration, forceCommit: true);

            return true;
        }

        _logger.LogCritical(
            exception,
            "Kafka consumer for topic {Topic} is stopping after a terminal failure in stage {Stage}. No dead-letter topic is configured.",
            registration.Topic,
            stage);

        return false;
    }

    private void CommitConsumedMessage(
        IConsumer<string?, string> consumer,
        ConsumeResult<string?, string> consumeResult,
        KafkaConsumerRegistration registration,
        bool forceCommit = false)
    {
        if (!forceCommit && registration.AutoCommit)
        {
            return;
        }

        try
        {
            consumer.Commit(consumeResult);
        }
        catch (KafkaException ex)
        {
            _logger.LogError(
                ex,
                "Failed to commit Kafka offset for topic {Topic}, partition {Partition}, offset {Offset}.",
                consumeResult.Topic,
                consumeResult.Partition.Value,
                consumeResult.Offset.Value);

            throw;
        }
    }

    private static TimeSpan CalculateRetryDelay(TimeSpan baseDelay, int retryAttempt)
    {
        if (baseDelay <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var multiplier = Math.Pow(2, retryAttempt - 1);
        var delayMilliseconds = baseDelay.TotalMilliseconds * multiplier;

        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }

    private static MessageContext CreateMessageContext(ConsumeResult<string?, string> consumeResult)
    {
        var headers = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? messageId = null;

        foreach (var header in consumeResult.Message.Headers ?? [])
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

        if (registration.MaxRetryAttempts < 0)
        {
            throw new InvalidOperationException("Kafka consumer max retry attempts must be zero or greater.");
        }
    }
}
