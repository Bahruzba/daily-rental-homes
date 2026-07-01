using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.FileUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100);
        builder.HasIndex(x => x.RentalHomeId);
        builder.HasIndex(x => x.BookingDepositId);
        builder.HasIndex(x => x.FileType);

        builder.HasOne(x => x.RentalHome)
            .WithMany(x => x.MediaFiles)
            .HasForeignKey(x => x.RentalHomeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<BookingDeposit>()
            .WithMany(x => x.ReceiptFiles)
            .HasForeignKey(x => x.BookingDepositId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
