using EventsService.Models;

namespace EventsService.Infrastructure.Abstractions;

public interface IPaymentEventsPublisher
{
    Task PublishAsync(PaymentEvent message, CancellationToken cancellationToken);
}