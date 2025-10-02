using System.Text.Json;
using Confluent.Kafka;
using EventsService.Infrastructure.Kafka.Configuration;
using EventsService.Infrastructure.Kafka.Consumers.Base;
using EventsService.Infrastructure.Kafka.Factory;
using EventsService.Models;
using Microsoft.Extensions.Options;

namespace EventsService.Infrastructure.Kafka.Consumers.MovieEvents;

internal sealed class MovieEventsConsumer : KafkaConsumerBackgroundService<long, string>
{
    private readonly ILogger<MovieEventsConsumer> _logger;

    public MovieEventsConsumer(
        IOptions<TopicConfiguration> topics,
        IOptions<KafkaConfiguration> configuration,
        IKafkaConsumerFactory kafkaConsumerFactory,
        ILogger<MovieEventsConsumer> logger) : base(
        topics.Value.MovieEventsTopicName, configuration, kafkaConsumerFactory, logger)
    {
        _logger = logger;
    }

    protected override Task HandleAsync(
        ConsumeResult<long, string> consumeResult,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<MovieEvent>(consumeResult.Message.Value);

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