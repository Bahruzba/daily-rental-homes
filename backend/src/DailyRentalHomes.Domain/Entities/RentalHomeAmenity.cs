using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class RentalHomeAmenity : BaseEntity
{
    public long RentalHomeId { get; set; }
    public long AmenityId { get; set; }

    public RentalHome? RentalHome { get; set; }
    public Amenity? Amenity { get; set; }
}
