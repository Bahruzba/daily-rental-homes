using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class BookingDepositConfiguration : IEntityTypeConfiguration<BookingDeposit>
{
    public void Configure(EntityTypeBuilder<BookingDeposit> builder)
    {
        builder.ToTable("booking_deposits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.HasIndex(x => x.BookingId).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}
