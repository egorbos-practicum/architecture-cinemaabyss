using System.Text.Json;
using EventsService.Infrastructure.Abstractions;
using EventsService.Infrastructure.Kafka.Configuration;
using EventsService.Infrastructure.Kafka.Factory;
using EventsService.Infrastructure.Kafka.Producers.Base;
using EventsService.Models;
using Microsoft.Extensions.Options;

namespace EventsService.Infrastructure.Kafka.Producers.UserEvents;

internal sealed class UserEventMessageProducer(
    IOptions<TopicConfiguration> topics,
    IOptions<KafkaConfiguration> configuration,
    IKafkaProducerFactory kafkaProducerFactory)
    : KafkaProducer<long, string>(topics.Value.UserEventsTopicName, configuration, kafkaProducerFactory),
        IUserEventsPublisher
{
    public async Task PublishAsync(UserEvent message, CancellationToken cancellationToken)
    {
        var value = JsonSerializer.Serialize(message);
        await ProduceAsync(message.UserId, value, cancellationToken);
    }
}