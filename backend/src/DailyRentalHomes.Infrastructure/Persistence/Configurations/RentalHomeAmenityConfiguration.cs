using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class RentalHomeAmenityConfiguration : IEntityTypeConfiguration<RentalHomeAmenity>
{
    public void Configure(EntityTypeBuilder<RentalHomeAmenity> builder)
    {
        builder.ToTable("rental_home_amenities");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RentalHomeId, x.AmenityId }).IsUnique();
    }
}
