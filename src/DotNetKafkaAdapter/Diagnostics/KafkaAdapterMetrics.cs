using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DotNetKafkaAdapter.Diagnostics;

internal static class KafkaAdapterMetrics
{
    private static readonly Meter Meter = new(KafkaAdapterInstrumentation.MeterName);
    private static readonly Counter<long> MessagesPublishedCounter =
        Meter.CreateCounter<long>(KafkaAdapterInstrumentation.MessagesPublished, description: "Number of successfully published Kafka messages.");
    private static readonly Counter<long> PublishFailuresCounter =
        Meter.CreateCounter<long>(KafkaAdapterInstrumentation.PublishFailures, description: "Number of Kafka publish failures.");
    private static readonly Histogram<double> PublishDurationHistogram =
        Meter.CreateHistogram<double>(KafkaAdapterInstrumentation.PublishDuration, unit: "ms", description: "Duration of Kafka publish operations.");
    private static readonly Counter<long> MessagesHandledCounter =
        Meter.CreateCounter<long>(KafkaAdapterInstrumentation.MessagesHandled, description: "Number of successfully handled Kafka messages.");
    private static readonly Counter<long> ConsumeFailuresCounter =
        Meter.CreateCounter<long>(KafkaAdapterInstrumentation.ConsumeFailures, description: "Number of Kafka consume failures.");
    private static readonly Counter<long> DeserializationFailuresCounter =
        Meter.CreateCounter<long>(KafkaAdapterInstrumentation.DeserializationFailures, description: "Number of Kafka message deserialization failures.");
    private static readonly Counter<long> HandlerFailuresCounter =
        Meter.CreateCounter<long>(KafkaAdapterInstrumentation.HandlerFailures, description: "Number of Kafka handler failures after retries are exhausted.");
    private static readonly Counter<long> RetryAttemptsCounter =
        Meter.CreateCounter<long>(KafkaAdapterInstrumentation.RetryAttempts, description: "Number of in-process Kafka handler retry attempts.");
    private static readonly Counter<long> DeadLetterMessagesCounter =
        Meter.CreateCounter<long>(KafkaAdapterInstrumentation.DeadLetterMessages, description: "Number of messages published to a dead-letter topic.");
    private static readonly Counter<long> OffsetCommitFailuresCounter =
        Meter.CreateCounter<long>(KafkaAdapterInstrumentation.OffsetCommitFailures, description: "Number of Kafka offset commit failures.");
    private static readonly UpDownCounter<long> ActiveConsumersCounter =
        Meter.CreateUpDownCounter<long>(KafkaAdapterInstrumentation.ActiveConsumers, description: "Number of currently active Kafka consumer loops.");

    public static IDisposable TrackPublishDuration(string topic)
        => new PublishDurationTracker(topic);

    public static void PublishSucceeded(string topic)
        => MessagesPublishedCounter.Add(1, CreateTopicTags(topic));

    public static void PublishFailed(string topic)
        => PublishFailuresCounter.Add(1, CreateTopicTags(topic));

    public static void ConsumerStarted(string topic, string consumerGroup)
        => ActiveConsumersCounter.Add(1, CreateConsumerTags(topic, consumerGroup));

    public static void ConsumerStopped(string topic, string consumerGroup)
        => ActiveConsumersCounter.Add(-1, CreateConsumerTags(topic, consumerGroup));

    public static void ConsumeFailed(string topic, string consumerGroup)
        => ConsumeFailuresCounter.Add(1, CreateConsumerTags(topic, consumerGroup));

    public static void DeserializationFailed(string topic, string consumerGroup)
        => DeserializationFailuresCounter.Add(1, CreateConsumerTags(topic, consumerGroup));

    public static void MessageHandled(string topic, string consumerGroup, string handlerType)
    {
        var tags = CreateConsumerTags(topic, consumerGroup);
        tags.Add("handler", handlerType);
        MessagesHandledCounter.Add(1, tags);
    }

    public static void RetryAttempted(string topic, string consumerGroup, string handlerType)
    {
        var tags = CreateConsumerTags(topic, consumerGroup);
        tags.Add("handler", handlerType);
        RetryAttemptsCounter.Add(1, tags);
    }

    public static void HandlerFailed(string topic, string consumerGroup, string handlerType)
    {
        var tags = CreateConsumerTags(topic, consumerGroup);
        tags.Add("handler", handlerType);
        HandlerFailuresCounter.Add(1, tags);
    }

    public static void DeadLetterPublished(string topic, string consumerGroup, string deadLetterTopic, string stage)
    {
        var tags = CreateConsumerTags(topic, consumerGroup);
        tags.Add("dead_letter_topic", deadLetterTopic);
        tags.Add("stage", stage);
        DeadLetterMessagesCounter.Add(1, tags);
    }

    public static void OffsetCommitFailed(string topic, string consumerGroup)
        => OffsetCommitFailuresCounter.Add(1, CreateConsumerTags(topic, consumerGroup));

    private static TagList CreateTopicTags(string topic)
    {
        var tags = new TagList
        {
            { "topic", topic }
        };

        return tags;
    }

    private static TagList CreateConsumerTags(string topic, string consumerGroup)
    {
        var tags = new TagList
        {
            { "topic", topic },
            { "consumer_group", consumerGroup }
        };

        return tags;
    }

    private sealed class PublishDurationTracker : IDisposable
    {
        private readonly string _topic;
        private readonly long _startTimestamp = Stopwatch.GetTimestamp();

        public PublishDurationTracker(string topic)
        {
            _topic = topic;
        }

        public void Dispose()
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;
            PublishDurationHistogram.Record(elapsedMilliseconds, CreateTopicTags(_topic));
        }
    }
}
