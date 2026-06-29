using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class PaymentCardConfiguration : IEntityTypeConfiguration<PaymentCard>
{
    public void Configure(EntityTypeBuilder<PaymentCard> builder)
    {
        builder.ToTable("payment_cards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CardHolderName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PanMasked).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.BrokerUserId);
    }
}
