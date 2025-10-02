namespace EventsService.Models;

public sealed record UserEvent(
    long UserId, string Username, string Action, DateTime Timestamp);