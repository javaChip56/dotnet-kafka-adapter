using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using DotNetKafkaAdapter.Abstractions;
using DotNetKafkaAdapter.Configuration;

namespace DotNetKafkaAdapter.Producing;

public sealed class KafkaMessagePublisher : IMessagePublisher, IDisposable
{
    private const string ContentTypeHeaderName = "content-type";
    private const string ContentTypeHeaderValue = "application/json";
    private const string MessageIdHeaderName = "message-id";

    private readonly KafkaAdapterOptions _options;
    private readonly IProducer<string?, string> _producer;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly bool _ownsProducer;

    public KafkaMessagePublisher(
        KafkaAdapterOptions options,
        JsonSerializerOptions? serializerOptions = null)
        : this(
            CreateProducer(options),
            options,
            serializerOptions,
            ownsProducer: true)
    {
    }

    public KafkaMessagePublisher(
        IProducer<string?, string> producer,
        KafkaAdapterOptions options,
        JsonSerializerOptions? serializerOptions = null)
        : this(
            producer,
            options,
            serializerOptions,
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

        await _producer
            .ProduceAsync(topic, kafkaMessage, cancellationToken)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (!_ownsProducer)
        {
            return;
        }

        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }

    private KafkaMessagePublisher(
        IProducer<string?, string> producer,
        KafkaAdapterOptions options,
        JsonSerializerOptions? serializerOptions,
        bool ownsProducer)
    {
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(options);

        _producer = producer;
        _options = options;
        _ownsProducer = ownsProducer;
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
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
