using EventsService.Models;

namespace EventsService.Infrastructure.Abstractions;

public interface IUserEventsPublisher
{
    Task PublishAsync(UserEvent message, CancellationToken cancellationToken);
}