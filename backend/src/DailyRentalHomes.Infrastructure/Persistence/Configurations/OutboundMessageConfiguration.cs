using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class OutboundMessageConfiguration : IEntityTypeConfiguration<OutboundMessage>
{
    public void Configure(EntityTypeBuilder<OutboundMessage> builder)
    {
        builder.ToTable("outbound_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.To).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Text).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ProviderMessageId).HasMaxLength(200);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        builder.HasIndex(x => x.Channel);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => x.BookingDepositId);

        builder.HasOne(x => x.Booking)
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.BookingDeposit)
            .WithMany()
            .HasForeignKey(x => x.BookingDepositId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
