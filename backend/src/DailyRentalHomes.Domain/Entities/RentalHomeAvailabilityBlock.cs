using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class RentalHomeAvailabilityBlock : BaseEntity
{
    public long RentalHomeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Note { get; set; }

    public RentalHome? RentalHome { get; set; }
}
