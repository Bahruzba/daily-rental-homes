using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class DistributedLockConfiguration : IEntityTypeConfiguration<DistributedLock>
{
    public void Configure(EntityTypeBuilder<DistributedLock> builder)
    {
        builder.ToTable("distributed_locks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwnerId).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
    }
}
