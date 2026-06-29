using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed class ChangeDepositStatusRequest
{
    public BookingDepositStatus Status { get; set; }
    public string? Note { get; set; }
}
