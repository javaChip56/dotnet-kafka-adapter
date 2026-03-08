using System.Text;
using System.Text.Json;
using System.Threading;
using Confluent.Kafka;
using DotNetKafkaAdapter.Abstractions;
using DotNetKafkaAdapter.Configuration;
using DotNetKafkaAdapter.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DotNetKafkaAdapter.Producing;

public sealed class KafkaMessagePublisher : IMessagePublisher, IDisposable
{
    private const string ContentTypeHeaderName = KafkaMessageHeaders.ContentType;
    private const string ContentTypeHeaderValue = "application/json";
    private const string MessageIdHeaderName = KafkaMessageHeaders.MessageId;

    private readonly KafkaAdapterOptions _options;
    private readonly IProducer<string?, string> _producer;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly bool _ownsProducer;
    private readonly ILogger<KafkaMessagePublisher>? _logger;
    private int _disposed;

    public KafkaMessagePublisher(
        KafkaAdapterOptions options,
        JsonSerializerOptions? serializerOptions = null,
        ILogger<KafkaMessagePublisher>? logger = null)
        : this(
            CreateProducer(options),
            options,
            serializerOptions,
            logger,
            ownsProducer: true)
    {
    }

    public KafkaMessagePublisher(
        IProducer<string?, string> producer,
        KafkaAdapterOptions options,
        JsonSerializerOptions? serializerOptions = null,
        ILogger<KafkaMessagePublisher>? logger = null)
        : this(
            producer,
            options,
            serializerOptions,
            logger,
            ownsProducer: false)
    {
    }

    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var defaultTopic = _options.Producer.DefaultTopic;
        if (string.IsNullOrWhiteSpace(defaultTopic))
        {
            throw new InvalidOperationException(
                "A default topic must be configured to publish messages without specifying a topic.");
        }

        return PublishAsync(defaultTopic, message, options: null, cancellationToken);
    }

    public async Task PublishAsync<TMessage>(
        string topic,
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(message);

        var kafkaMessage = new Message<string?, string>
        {
            Key = options?.Key,
            Value = JsonSerializer.Serialize(message, _serializerOptions),
            Headers = CreateHeaders(options)
        };

        using var publishTimer = KafkaAdapterMetrics.TrackPublishDuration(topic);

        try
        {
            var deliveryResult = await _producer
                .ProduceAsync(topic, kafkaMessage, cancellationToken)
                .ConfigureAwait(false);

            KafkaAdapterMetrics.PublishSucceeded(topic);
            _logger?.LogDebug(
                KafkaAdapterLogEvents.PublishSucceeded,
                "Published Kafka message to topic {Topic} partition {Partition} offset {Offset}.",
                deliveryResult.Topic,
                deliveryResult.Partition.Value,
                deliveryResult.Offset.Value);
        }
        catch (ProduceException<string?, string> ex)
        {
            KafkaAdapterMetrics.PublishFailed(topic);
            _logger?.LogError(
                KafkaAdapterLogEvents.PublishFailed,
                ex,
                "Failed to publish Kafka message to topic {Topic}.",
                topic);

            throw;
        }
        catch (Exception)
        {
            KafkaAdapterMetrics.PublishFailed(topic);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_ownsProducer || Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        try
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            try
            {
                _producer.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private KafkaMessagePublisher(
        IProducer<string?, string> producer,
        KafkaAdapterOptions options,
        JsonSerializerOptions? serializerOptions,
        ILogger<KafkaMessagePublisher>? logger,
        bool ownsProducer)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(options);

        _producer = producer;
        _options = options;
        _ownsProducer = ownsProducer;
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _logger = logger;
    }

    private static Headers CreateHeaders(PublishOptions? options)
    {
        var headers = new Headers();

        headers.Add(ContentTypeHeaderName, Encoding.UTF8.GetBytes(ContentTypeHeaderValue));

        if (!string.IsNullOrWhiteSpace(options?.MessageId))
        {
            headers.Add(MessageIdHeaderName, Encoding.UTF8.GetBytes(options.MessageId));
        }

        if (options?.Headers is null)
        {
            return headers;
        }

        foreach (var header in options.Headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key))
            {
                continue;
            }

            headers.Add(header.Key, header.Value is null ? null : Encoding.UTF8.GetBytes(header.Value));
        }

        return headers;
    }

    private static IProducer<string?, string> CreateProducer(KafkaAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new ProducerBuilder<string?, string>(KafkaProducerConfigFactory.Create(options)).Build();
    }
}
