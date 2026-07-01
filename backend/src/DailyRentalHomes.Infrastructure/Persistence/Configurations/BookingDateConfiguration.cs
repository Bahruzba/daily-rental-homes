using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class BookingDateConfiguration : IEntityTypeConfiguration<BookingDate>
{
    public void Configure(EntityTypeBuilder<BookingDate> builder)
    {
        builder.ToTable("booking_dates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Date).IsRequired();
        builder.HasIndex(x => new { x.BookingId, x.Date }).IsUnique();

        builder.HasOne(x => x.Booking)
            .WithMany(x => x.Dates)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
