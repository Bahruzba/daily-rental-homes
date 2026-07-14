using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class BookingCancellationRequestConfiguration : IEntityTypeConfiguration<BookingCancellationRequest>
{
    public void Configure(EntityTypeBuilder<BookingCancellationRequest> builder)
    {
        builder.ToTable("booking_cancellation_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.StatusCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DecisionNote).HasMaxLength(1000);
        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => x.RequestedByUserId);
        builder.HasIndex(x => x.StatusCode);

        builder.HasOne(x => x.Booking)
            .WithMany(x => x.CancellationRequests)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
