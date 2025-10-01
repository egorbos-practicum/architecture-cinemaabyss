using Confluent.Kafka;

namespace EventsService.Infrastructure.Kafka.Factory;

internal sealed class KafkaFactory(ILogger<KafkaFactory> logger) : IKafkaConsumerFactory, IKafkaProducerFactory
{
    IConsumer<TKey, TValue> IKafkaConsumerFactory.Build<TKey, TValue>(Action<ConsumerConfig> configAction)
    {
        var config = new ConsumerConfig();
        configAction(config);
        var consumerBuilder = new ConsumerBuilder<TKey, TValue>(config);
        return consumerBuilder
            .SetErrorHandler((_, error) => { logger.LogError(error.Reason); })
            .SetLogHandler((_, message) => logger.LogInformation(message.Message))
            .Build();
    }

    IProducer<TKey, TValue> IKafkaProducerFactory.Build<TKey, TValue>(Action<ProducerConfig> configAction)
    {
        var config = new ProducerConfig();
        configAction(config);
        var producerBuilder = new ProducerBuilder<TKey, TValue>(config);
        return producerBuilder
            .SetErrorHandler((_, error) => { logger.LogError(error.Reason); })
            .SetLogHandler((_, message) => logger.LogInformation(message.Message))
            .Build();
    }
}