using Confluent.Kafka;

namespace EventsService.Infrastructure.Kafka.Factory;

internal interface IKafkaConsumerFactory
{
    IConsumer<TKey, TValue> Build<TKey, TValue>(Action<ConsumerConfig> configAction);
}