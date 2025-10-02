using System.Text.Json;
using Confluent.Kafka;
using EventsService.Infrastructure.Kafka.Configuration;
using EventsService.Infrastructure.Kafka.Consumers.Base;
using EventsService.Infrastructure.Kafka.Factory;
using EventsService.Models;
using Microsoft.Extensions.Options;

namespace EventsService.Infrastructure.Kafka.Consumers.PaymentEvents;

internal sealed class PaymentEventsConsumer : KafkaConsumerBackgroundService<long, string>
{
    private readonly ILogger<PaymentEventsConsumer> _logger;

    public PaymentEventsConsumer(
        IOptions<TopicConfiguration> topics,
        IOptions<KafkaConfiguration> configuration,
        IKafkaConsumerFactory kafkaConsumerFactory,
        ILogger<PaymentEventsConsumer> logger) : base(
        topics.Value.PaymentEventsTopicName, configuration, kafkaConsumerFactory, logger)
    {
        _logger = logger;
    }

    protected override Task HandleAsync(
        ConsumeResult<long, string> consumeResult,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<PaymentEvent>(consumeResult.Message.Value);

            if (message is null)
            {
                _logger.LogWarning("Deserializing error: {@Message}", consumeResult.Message.Value);
                return Task.CompletedTask;
            }

            _logger.LogInformation("Processing event with offset {@Offset}: {@Event}", consumeResult.Offset.Value, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handling error: {@ConsumeResult}", consumeResult);
        }
        return Task.CompletedTask;
    }
}