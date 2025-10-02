using System.Text.Json;
using EventsService.Infrastructure.Abstractions;
using EventsService.Infrastructure.Kafka.Configuration;
using EventsService.Infrastructure.Kafka.Factory;
using EventsService.Infrastructure.Kafka.Producers.Base;
using EventsService.Models;
using Microsoft.Extensions.Options;

namespace EventsService.Infrastructure.Kafka.Producers.PaymentEvents;

internal sealed class PaymentEventMessageProducer(
    IOptions<TopicConfiguration> topics,
    IOptions<KafkaConfiguration> configuration,
    IKafkaProducerFactory kafkaProducerFactory)
    : KafkaProducer<long, string>(topics.Value.PaymentEventsTopicName, configuration, kafkaProducerFactory),
        IPaymentEventsPublisher
{
    public async Task PublishAsync(PaymentEvent message, CancellationToken cancellationToken)
    {
        var value = JsonSerializer.Serialize(message);
        await ProduceAsync(message.PaymentId, value, cancellationToken);
    }
}