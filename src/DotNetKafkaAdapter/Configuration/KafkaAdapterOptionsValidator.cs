using Microsoft.Extensions.Options;

namespace DotNetKafkaAdapter.Configuration;

internal sealed class KafkaAdapterOptionsValidator : IValidateOptions<KafkaAdapterOptions>
{
    public ValidateOptionsResult Validate(string? name, KafkaAdapterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            failures.Add("Kafka BootstrapServers must be configured.");
        }

        if (options.Producer.DefaultTopic is not null && string.IsNullOrWhiteSpace(options.Producer.DefaultTopic))
        {
            failures.Add("Kafka Producer.DefaultTopic cannot be empty or whitespace.");
        }

        ValidateSecurity(options, failures);
        ValidateConsumers(options, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateSecurity(KafkaAdapterOptions options, List<string> failures)
    {
        var protocol = options.Security.Protocol;
        var requiresSasl = protocol is KafkaSecurityProtocol.SaslPlaintext or KafkaSecurityProtocol.SaslSsl;

        if (requiresSasl)
        {
            if (string.IsNullOrWhiteSpace(options.Security.Username))
            {
                failures.Add($"Kafka Security.Username must be configured when using {protocol}.");
            }

            if (string.IsNullOrWhiteSpace(options.Security.Password))
            {
                failures.Add($"Kafka Security.Password must be configured when using {protocol}.");
            }
        }
    }

    private static void ValidateConsumers(KafkaAdapterOptions options, List<string> failures)
    {
        for (var index = 0; index < options.Consumers.Count; index++)
        {
            var consumer = options.Consumers[index];

            if (string.IsNullOrWhiteSpace(consumer.Topic))
            {
                failures.Add($"Kafka Consumers[{index}].Topic must be configured.");
            }

            if (string.IsNullOrWhiteSpace(consumer.ConsumerGroup))
            {
                failures.Add($"Kafka Consumers[{index}].ConsumerGroup must be configured.");
            }

            if (consumer.MessageType == typeof(object))
            {
                failures.Add($"Kafka Consumers[{index}].MessageType must be configured.");
            }

            if (consumer.HandlerType == typeof(object))
            {
                failures.Add($"Kafka Consumers[{index}].HandlerType must be configured.");
            }

            if (consumer.MaxRetryAttempts < 0)
            {
                failures.Add($"Kafka Consumers[{index}].MaxRetryAttempts must be zero or greater.");
            }

            if (consumer.DeadLetterTopic is not null && string.IsNullOrWhiteSpace(consumer.DeadLetterTopic))
            {
                failures.Add($"Kafka Consumers[{index}].DeadLetterTopic cannot be empty or whitespace.");
            }
        }
    }
}
