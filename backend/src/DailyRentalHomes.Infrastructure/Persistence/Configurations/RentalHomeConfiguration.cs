using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class RentalHomeConfiguration : IEntityTypeConfiguration<RentalHome>
{
    public void Configure(EntityTypeBuilder<RentalHome> builder)
    {
        builder.ToTable("rental_homes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.District).HasMaxLength(100);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.DailyPrice).HasPrecision(18, 2);
        builder.HasIndex(x => x.City);
        builder.HasIndex(x => x.BrokerUserId);

        builder.HasOne(x => x.BrokerUser)
            .WithMany()
            .HasForeignKey(x => x.BrokerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
