using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class BookingStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
