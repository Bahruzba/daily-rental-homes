using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DailyRentalHomes.Infrastructure.Persistence.Configurations;

public sealed class UserMap : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.PhoneNumber).IsUnique();
    }
}
