using DailyRentalHomes.Domain.Common;
using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Domain.Entities;

public sealed class RelatedContact : BaseEntity
{
    public long RentalHomeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public ContactType ContactType { get; set; }
    public bool NotifyEnabled { get; set; }

    public RentalHome? RentalHome { get; set; }
}
