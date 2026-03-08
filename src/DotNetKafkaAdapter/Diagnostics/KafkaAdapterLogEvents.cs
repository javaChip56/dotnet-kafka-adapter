using Microsoft.Extensions.Logging;

namespace DotNetKafkaAdapter.Diagnostics;

internal static class KafkaAdapterLogEvents
{
    public static readonly EventId NoConsumerRegistrations = new(1000, nameof(NoConsumerRegistrations));
    public static readonly EventId ConsumerStarting = new(1001, nameof(ConsumerStarting));
    public static readonly EventId ConsumerStopping = new(1002, nameof(ConsumerStopping));
    public static readonly EventId ConsumeFailure = new(1003, nameof(ConsumeFailure));
    public static readonly EventId DeserializationFailure = new(1004, nameof(DeserializationFailure));
    public static readonly EventId HandlerRetry = new(1005, nameof(HandlerRetry));
    public static readonly EventId HandlerFailure = new(1006, nameof(HandlerFailure));
    public static readonly EventId DeadLetterPublish = new(1007, nameof(DeadLetterPublish));
    public static readonly EventId TerminalFailure = new(1008, nameof(TerminalFailure));
    public static readonly EventId OffsetCommitFailure = new(1009, nameof(OffsetCommitFailure));
    public static readonly EventId PublishSucceeded = new(1010, nameof(PublishSucceeded));
    public static readonly EventId PublishFailed = new(1011, nameof(PublishFailed));
}
