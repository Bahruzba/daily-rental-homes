using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class Amenity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? IconName { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RentalHomeAmenity> RentalHomes { get; set; } = new List<RentalHomeAmenity>();
}
