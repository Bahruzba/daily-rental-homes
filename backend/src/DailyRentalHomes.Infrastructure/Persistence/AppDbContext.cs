using DailyRentalHomes.Application.Abstractions.Persistence;
using DailyRentalHomes.Domain.Common;
using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text;

namespace DailyRentalHomes.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<RentalHome> RentalHomes => Set<RentalHome>();
    public DbSet<RentalHomeAvailabilityBlock> RentalHomeAvailabilityBlocks => Set<RentalHomeAvailabilityBlock>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<RelatedContact> RelatedContacts => Set<RelatedContact>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<RentalHomeAmenity> RentalHomeAmenities => Set<RentalHomeAmenity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingDate> BookingDates => Set<BookingDate>();
    public DbSet<BookingDeposit> BookingDeposits => Set<BookingDeposit>();
    public DbSet<BookingExpense> BookingExpenses => Set<BookingExpense>();
    public DbSet<BookingCancellationRequest> BookingCancellationRequests => Set<BookingCancellationRequest>();
    public DbSet<BookingStatus> BookingStatuses => Set<BookingStatus>();
    public DbSet<BookingStatusHistory> BookingStatusHistory => Set<BookingStatusHistory>();
    public DbSet<PaymentCard> PaymentCards => Set<PaymentCard>();
    public DbSet<OutboundMessage> OutboundMessages => Set<OutboundMessage>();
    public DbSet<DistributedLock> DistributedLocks => Set<DistributedLock>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyEntityStateRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyEntityStateRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplySnakeCaseNames(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);
    }

    private static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is not null)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private void ApplyEntityStateRules()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(x => typeof(BaseEntity).IsAssignableFrom(x.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeleted = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(isDeleted), parameter);
            entityType.SetQueryFilter(filter);
        }
    }

    private static string ToSnakeCase(string value)
    {
        var result = new StringBuilder(value.Length + 8);

        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (char.IsUpper(current) && index > 0)
            {
                var previous = value[index - 1];
                var nextIsLower = index + 1 < value.Length && char.IsLower(value[index + 1]);
                if (char.IsLower(previous) || char.IsDigit(previous) || nextIsLower)
                {
                    result.Append('_');
                }
            }

            result.Append(char.ToLowerInvariant(current));
        }

        return result.ToString();
    }
}
