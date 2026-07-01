using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class BookingStatusHistoryConfiguration : IEntityTypeConfiguration<BookingStatusHistory>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistory> builder)
    {
        builder.ToTable("booking_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => x.NewStatusId);

        builder.HasOne(x => x.Booking)
            .WithMany(x => x.StatusHistory)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OldStatus)
            .WithMany()
            .HasForeignKey(x => x.OldStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.NewStatus)
            .WithMany()
            .HasForeignKey(x => x.NewStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ChangedByUser)
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
