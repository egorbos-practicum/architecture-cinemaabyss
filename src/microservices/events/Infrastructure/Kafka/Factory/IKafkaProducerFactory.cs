using Confluent.Kafka;

namespace EventsService.Infrastructure.Kafka.Factory;

internal interface IKafkaProducerFactory
{
    IProducer<TKey, TValue> Build<TKey, TValue>(Action<ProducerConfig> configAction);
}