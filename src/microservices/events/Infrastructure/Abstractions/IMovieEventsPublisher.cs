using EventsService.Models;

namespace EventsService.Infrastructure.Abstractions;

public interface IMovieEventsPublisher
{
    Task PublishAsync(MovieEvent message, CancellationToken cancellationToken);
}