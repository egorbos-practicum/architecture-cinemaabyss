namespace EventsService.Infrastructure.Kafka.Configuration;

internal sealed class TopicConfiguration
{
    public string MovieEventsTopicName { get; set; } = string.Empty;
    public string UserEventsTopicName { get; set; } = string.Empty;
    public string PaymentEventsTopicName { get; set; } = string.Empty;
}