using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class RentalHomeAvailabilityBlockConfiguration : IEntityTypeConfiguration<RentalHomeAvailabilityBlock>
{
    public void Configure(EntityTypeBuilder<RentalHomeAvailabilityBlock> builder)
    {
        builder.ToTable("rental_home_availability_blocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => x.RentalHomeId);
        builder.HasIndex(x => new { x.RentalHomeId, x.StartDate, x.EndDate });

        builder.HasOne(x => x.RentalHome)
            .WithMany(x => x.AvailabilityBlocks)
            .HasForeignKey(x => x.RentalHomeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
