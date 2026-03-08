using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotNetKafkaAdapter.Abstractions;
using DotNetKafkaAdapter.Configuration;
using DotNetKafkaAdapter.Consuming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetKafkaAdapter.IntegrationTests;

public sealed class KafkaAdapterIntegrationTests
{
    private const string BootstrapServers = "localhost:9092";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PublishAsync_ShouldDeliverMessageToRegisteredHandler()
    {
        var topic = CreateTopicName("publish-consume");
        var consumerGroup = CreateConsumerGroup("publish-consume");

        await EnsureTopicsExistAsync(topic);

        var sink = new MessageSink<TestMessage>();

        using var host = CreateHost(
            topic,
            services =>
            {
                services.AddSingleton(sink);
                services.AddKafkaHandler<TestMessage, RecordingMessageHandler>(topic, consumerGroup);
            });

        await host.StartAsync();

        var publisher = host.Services.GetRequiredService<IMessagePublisher>();
        var message = new TestMessage(Guid.NewGuid().ToString("N"), "hello from integration test");

        await publisher.PublishAsync(
            topic,
            message,
            new PublishOptions
            {
                Key = message.Id,
                MessageId = message.Id
            });

        var received = await sink.WaitForMessageAsync(TimeSpan.FromSeconds(30));

        Assert.NotNull(received);
        Assert.Equal(message.Id, received.Id);
        Assert.Equal(message.Value, received.Value);

        await host.StopAsync();
    }

    [Fact]
    public async Task PublishAsync_ShouldRetryAndPublishToDeadLetterTopicWhenHandlerFails()
    {
        var topic = CreateTopicName("dead-letter");
        var deadLetterTopic = CreateTopicName("dead-letter-output");
        var consumerGroup = CreateConsumerGroup("dead-letter");

        await EnsureTopicsExistAsync(topic, deadLetterTopic);

        var attempts = new FailureAttemptCounter();

        using var host = CreateHost(
            topic,
            services =>
            {
                services.AddSingleton(attempts);
                services.AddKafkaHandler<TestMessage, AlwaysFailingMessageHandler>(
                    topic,
                    consumerGroup,
                    registration =>
                    {
                        registration.MaxRetryAttempts = 2;
                        registration.RetryDelay = TimeSpan.FromMilliseconds(200);
                        registration.DeadLetterTopic = deadLetterTopic;
                    });
            });

        await host.StartAsync();

        var publisher = host.Services.GetRequiredService<IMessagePublisher>();
        var message = new TestMessage(Guid.NewGuid().ToString("N"), "poison message");

        await publisher.PublishAsync(
            topic,
            message,
            new PublishOptions
            {
                Key = message.Id,
                MessageId = message.Id
            });

        var deadLetterPayload = await ConsumeSingleMessageAsync(deadLetterTopic, CreateConsumerGroup("dlq-reader"));
        var deadLetterMessage = JsonSerializer.Deserialize<KafkaDeadLetterMessage>(deadLetterPayload, JsonOptions);

        Assert.NotNull(deadLetterMessage);
        Assert.Equal(topic, deadLetterMessage.OriginalTopic);
        Assert.Equal(consumerGroup, deadLetterMessage.ConsumerGroup);
        Assert.Equal(message.Id, deadLetterMessage.MessageId);
        Assert.Contains("poison message", deadLetterMessage.Payload);
        Assert.Equal("handler", deadLetterMessage.FailureStage);
        Assert.Equal(3, attempts.Value);

        await host.StopAsync();
    }

    private static IHost CreateHost(string defaultTopic, Action<IServiceCollection> configureServices)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(options => options.SingleLine = true);
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .ConfigureServices(services =>
            {
                services.AddKafkaAdapter(options =>
                {
                    options.BootstrapServers = BootstrapServers;
                    options.ClientId = $"integration-tests-{Guid.NewGuid():N}";
                    options.Producer.DefaultTopic = defaultTopic;
                });

                configureServices(services);
            })
            .Build();
    }

    private static async Task EnsureTopicsExistAsync(params string[] topics)
    {
        await WaitForBrokerAsync();

        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = BootstrapServers
        }).Build();

        try
        {
            await adminClient.CreateTopicsAsync(
                topics.Select(topic => new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }));
        }
        catch (CreateTopicsException ex) when (ex.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
        }
    }

    private static async Task WaitForBrokerAsync()
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = BootstrapServers
        }).Build();

        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(30);

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            try
            {
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(2));
                if (metadata.Brokers.Count > 0)
                {
                    return;
                }
            }
            catch (KafkaException)
            {
            }

            await Task.Delay(500);
        }

        throw new TimeoutException("Kafka broker at localhost:9092 did not become ready in time.");
    }

    private static Task<string> ConsumeSingleMessageAsync(string topic, string consumerGroup)
    {
        using var consumer = new ConsumerBuilder<string?, string>(
            new ConsumerConfig
            {
                BootstrapServers = BootstrapServers,
                GroupId = consumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            })
            .Build();

        consumer.Subscribe(topic);

        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(30);

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (consumeResult?.Message?.Value is { } payload)
            {
                consumer.Commit(consumeResult);
                consumer.Close();
                return Task.FromResult(payload);
            }
        }

        consumer.Close();
        throw new TimeoutException($"No message was received from topic '{topic}' within the expected time.");
    }

    private static string CreateTopicName(string prefix)
        => $"dotnet-kafka-adapter-{prefix}-{Guid.NewGuid():N}";

    private static string CreateConsumerGroup(string prefix)
        => $"dotnet-kafka-adapter-{prefix}-{Guid.NewGuid():N}";

    private sealed record TestMessage(string Id, string Value);

    private sealed class RecordingMessageHandler(MessageSink<TestMessage> sink) : IMessageHandler<TestMessage>
    {
        public Task HandleAsync(
            MessageContext context,
            TestMessage message,
            CancellationToken cancellationToken = default)
        {
            sink.Record(message);
            return Task.CompletedTask;
        }
    }

    private sealed class AlwaysFailingMessageHandler(FailureAttemptCounter attempts) : IMessageHandler<TestMessage>
    {
        public Task HandleAsync(
            MessageContext context,
            TestMessage message,
            CancellationToken cancellationToken = default)
        {
            attempts.Increment();
            throw new InvalidOperationException("Intentional handler failure for integration testing.");
        }
    }

    private sealed class MessageSink<TMessage>
    {
        private readonly TaskCompletionSource<TMessage> _completionSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(TMessage message)
        {
            _completionSource.TrySetResult(message);
        }

        public async Task<TMessage> WaitForMessageAsync(TimeSpan timeout)
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource(timeout);
            using var registration = timeoutCancellationTokenSource.Token.Register(
                () => _completionSource.TrySetException(new TimeoutException("Timed out waiting for a consumed message.")));

            return await _completionSource.Task;
        }
    }

    private sealed class FailureAttemptCounter
    {
        private int _value;

        public int Value => Volatile.Read(ref _value);

        public void Increment()
        {
            Interlocked.Increment(ref _value);
        }
    }
}
