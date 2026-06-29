using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed class UpdateDepositStatusInput
{
    public BookingDepositStatus Status { get; set; }
    public string? Note { get; set; }
}
