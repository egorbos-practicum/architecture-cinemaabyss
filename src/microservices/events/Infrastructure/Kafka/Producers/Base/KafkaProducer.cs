using Confluent.Kafka;
using EventsService.Infrastructure.Kafka.Configuration;
using EventsService.Infrastructure.Kafka.Factory;
using Microsoft.Extensions.Options;

namespace EventsService.Infrastructure.Kafka.Producers.Base;

internal abstract class KafkaProducer<TKey, TValue>
{
    public string Topic { get; init; }

    protected readonly IProducer<TKey, TValue> Producer;

    public KafkaProducer(
        string topic,
        IOptions<KafkaConfiguration> configuration,
        IKafkaProducerFactory kafkaProducerFactory)
    {
        Topic = topic;
        Producer = kafkaProducerFactory
            .Build<TKey, TValue>(config => {
                config.AllowAutoCreateTopics = true;
                config.BootstrapServers = configuration.Value.Servers;
            });
    }

    protected virtual async Task ProduceAsync(
        TKey key, TValue value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = new Message<TKey, TValue>
        {
            Key = key,
            Value = value
        };

        await Producer.ProduceAsync(Topic, message, cancellationToken);
    }
}
