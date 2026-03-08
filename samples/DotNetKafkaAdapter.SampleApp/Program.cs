using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotNetKafkaAdapter.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
var topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "sample.orders";
var consumerGroup = Environment.GetEnvironmentVariable("KAFKA_CONSUMER_GROUP") ?? "sample.orders.consumer";
var deadLetterTopic = Environment.GetEnvironmentVariable("KAFKA_DEAD_LETTER_TOPIC") ?? $"{topic}.dlq";

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddSingleton(new SampleSettings(
    bootstrapServers,
    topic,
    consumerGroup,
    deadLetterTopic));

builder.Services.AddKafkaAdapter(options =>
{
    options.BootstrapServers = bootstrapServers;
    options.ClientId = "dotnet-kafka-adapter-sample-app";
    options.Producer.DefaultTopic = topic;
});

builder.Services.AddKafkaHandler<OrderSubmitted, OrderSubmittedHandler>(
    topic,
    consumerGroup,
    registration =>
    {
        registration.MaxRetryAttempts = 1;
        registration.RetryDelay = TimeSpan.FromSeconds(1);
        registration.DeadLetterTopic = deadLetterTopic;
    });

builder.Services.AddHostedService<SampleTopicBootstrapper>();
builder.Services.AddHostedService<SamplePublisherService>();

await builder.Build().RunAsync();

internal sealed record SampleSettings(
    string BootstrapServers,
    string Topic,
    string ConsumerGroup,
    string DeadLetterTopic);

internal sealed record OrderSubmitted(
    string OrderId,
    string CustomerId,
    decimal Total);

internal sealed class SampleTopicBootstrapper(
    SampleSettings settings,
    ILogger<SampleTopicBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = settings.BootstrapServers
        }).Build();

        try
        {
            await adminClient.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = settings.Topic,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    },
                    new TopicSpecification
                    {
                        Name = settings.DeadLetterTopic,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    }
                ]);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            logger.LogInformation(
                "Kafka topics {Topic} and {DeadLetterTopic} already exist.",
                settings.Topic,
                settings.DeadLetterTopic);
            return;
        }

        logger.LogInformation(
            "Created Kafka topics {Topic} and {DeadLetterTopic}.",
            settings.Topic,
            settings.DeadLetterTopic);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class SamplePublisherService(
    IMessagePublisher publisher,
    SampleSettings settings,
    IHostApplicationLifetime lifetime,
    ILogger<SamplePublisherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        var message = new OrderSubmitted(
            OrderId: $"order-{Guid.NewGuid():N}",
            CustomerId: "customer-123",
            Total: 42.50m);

        await publisher.PublishAsync(
            settings.Topic,
            message,
            new PublishOptions
            {
                Key = message.OrderId,
                MessageId = message.OrderId
            },
            stoppingToken);

        logger.LogInformation(
            "Published sample order {OrderId} to topic {Topic}. Waiting for the consumer handler.",
            message.OrderId,
            settings.Topic);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogWarning("Timed out waiting for the sample message to be consumed. Stopping the sample app.");
        lifetime.StopApplication();
    }
}

internal sealed class OrderSubmittedHandler(
    IHostApplicationLifetime lifetime,
    ILogger<OrderSubmittedHandler> logger) : IMessageHandler<OrderSubmitted>
{
    public Task HandleAsync(
        MessageContext context,
        OrderSubmitted message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Consumed order {OrderId} for customer {CustomerId} from topic {Topic} at offset {Offset}.",
            message.OrderId,
            message.CustomerId,
            context.Topic,
            context.Offset);

        lifetime.StopApplication();
        return Task.CompletedTask;
    }
}
