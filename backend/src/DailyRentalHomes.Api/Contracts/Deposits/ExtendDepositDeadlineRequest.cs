namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed class ExtendDepositDeadlineRequest
{
    public DateTime DeadlineAt { get; set; }
    public string? Reason { get; set; }
}

public sealed record ExtendDepositDeadlineResponse(
    long BookingId,
    long DepositId,
    DateTime DeadlineAt,
    DateTime DeadlineExtendedAt,
    string? DeadlineExtensionReason);
