using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class BookingExpenseConfiguration : IEntityTypeConfiguration<BookingExpense>
{
    public void Configure(EntityTypeBuilder<BookingExpense> builder)
    {
        builder.ToTable("booking_expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TypeCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => x.TypeCode);

        builder.HasOne(x => x.Booking)
            .WithMany(x => x.Expenses)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
