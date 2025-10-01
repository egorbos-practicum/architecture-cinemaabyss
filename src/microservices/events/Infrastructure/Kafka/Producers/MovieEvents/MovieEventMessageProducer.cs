using System.Text.Json;
using EventsService.Infrastructure.Abstractions;
using EventsService.Infrastructure.Kafka.Configuration;
using EventsService.Infrastructure.Kafka.Factory;
using EventsService.Infrastructure.Kafka.Producers.Base;
using EventsService.Models;
using Microsoft.Extensions.Options;

namespace EventsService.Infrastructure.Kafka.Producers.MovieEvents;

internal sealed class MovieEventMessageProducer(
    IOptions<TopicConfiguration> topics,
    IOptions<KafkaConfiguration> configuration,
    IKafkaProducerFactory kafkaProducerFactory)
    : KafkaProducer<long, string>(topics.Value.MovieEventsTopicName, configuration, kafkaProducerFactory),
        IMovieEventsPublisher
{
    public async Task PublishAsync(MovieEvent message, CancellationToken cancellationToken)
    {
        var value = JsonSerializer.Serialize(message);
        await ProduceAsync(message.MovieId, value, cancellationToken);
    }
}