using Confluent.Kafka;
using EventsService.Infrastructure.Kafka.Configuration;
using EventsService.Infrastructure.Kafka.Factory;
using Microsoft.Extensions.Options;

namespace EventsService.Infrastructure.Kafka.Consumers.Base;

internal abstract class KafkaConsumerBackgroundService<TKey, TValue> : BackgroundService
{
    public string Topic { get; init; }

    private const int ConsumeTimeout = 100;
    protected readonly IConsumer<TKey, TValue> Consumer;
    private readonly ILogger<KafkaConsumerBackgroundService<TKey, TValue>> _logger;

    public KafkaConsumerBackgroundService(
        string topic,
        IOptions<KafkaConfiguration> configuration,
        IKafkaConsumerFactory kafkaConsumerFactory,
        ILogger<KafkaConsumerBackgroundService<TKey, TValue>> logger)
    {
        Topic = topic;
        _logger = logger;
        Consumer = kafkaConsumerFactory
            .Build<TKey, TValue>(config => {
                config.EnableAutoCommit = false;
                config.GroupId = $"{Topic}_group";
                config.AllowAutoCreateTopics = true;
                config.AutoOffsetReset = AutoOffsetReset.Earliest;
                config.BootstrapServers = configuration.Value.Servers;
            });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        
        Consumer.Subscribe(Topic);

        _logger.LogInformation("Start consumer topic {Topic}", Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ConsumeAsync(stoppingToken);
        }

        Consumer.Unsubscribe();

        _logger.LogInformation("Stop consumer topic {Topic}", Topic);
    }

    protected virtual async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var consumeResult = Consumer.Consume(TimeSpan.FromMilliseconds(ConsumeTimeout));

            if (consumeResult is not null)
            {
                await HandleAsync(consumeResult, cancellationToken);
                Consumer.Commit();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message.");
        }
    }

    protected abstract Task HandleAsync(
        ConsumeResult<TKey, TValue> consumeResult,
        CancellationToken cancellationToken);

    public override void Dispose()
    {
        Consumer?.Close();
        Consumer?.Dispose();
        base.Dispose();
    }
}
