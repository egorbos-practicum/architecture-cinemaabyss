namespace EventsService.Models;

public sealed record PaymentEvent(
    long PaymentId, long UserId, double Amount,
    string Status, DateTime Timestamp, string MethodType);