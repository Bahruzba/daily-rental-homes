using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerFullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CustomerPhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DailyPrice).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.Source).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.RentalHomeId);
        builder.HasIndex(x => x.StatusId);

        builder.HasOne(x => x.RentalHome)
            .WithMany()
            .HasForeignKey(x => x.RentalHomeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CustomerUser)
            .WithMany()
            .HasForeignKey(x => x.CustomerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
