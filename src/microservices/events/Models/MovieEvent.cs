namespace EventsService.Models;

public sealed record MovieEvent(
    long MovieId, string Title, string Action, long UserId);