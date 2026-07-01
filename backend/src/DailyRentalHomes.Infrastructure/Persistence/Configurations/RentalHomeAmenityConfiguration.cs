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

        builder.HasOne(x => x.RentalHome)
            .WithMany(x => x.Amenities)
            .HasForeignKey(x => x.RentalHomeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Amenity)
            .WithMany(x => x.RentalHomes)
            .HasForeignKey(x => x.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
