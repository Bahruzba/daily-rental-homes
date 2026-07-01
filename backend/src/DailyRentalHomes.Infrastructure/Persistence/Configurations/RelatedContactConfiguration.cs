using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class RelatedContactConfiguration : IEntityTypeConfiguration<RelatedContact>
{
    public void Configure(EntityTypeBuilder<RelatedContact> builder)
    {
        builder.ToTable("related_contacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.RentalHomeId);
        builder.HasIndex(x => x.ContactType);

        builder.HasOne(x => x.RentalHome)
            .WithMany(x => x.Contacts)
            .HasForeignKey(x => x.RentalHomeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
