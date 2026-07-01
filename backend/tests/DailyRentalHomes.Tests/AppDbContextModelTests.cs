using DailyRentalHomes.Domain.Common;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DailyRentalHomes.Tests;

public sealed class AppDbContextModelTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelTests;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Model_UsesSnakeCaseTablesAndColumns()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Booking));

        Assert.NotNull(entity);
        Assert.Equal("bookings", entity.GetTableName());

        var table = StoreObjectIdentifier.Table("bookings", null);
        Assert.Equal("customer_phone_number", entity.FindProperty(nameof(Booking.CustomerPhoneNumber))?.GetColumnName(table));
        Assert.Equal("created_by_user_id", entity.FindProperty(nameof(BaseEntity.CreatedByUserId))?.GetColumnName(table));
    }

    [Fact]
    public void Model_AppliesSoftDeleteFilterToEveryBaseEntity()
    {
        using var context = CreateContext();

        var entityTypes = context.Model.GetEntityTypes()
            .Where(x => typeof(BaseEntity).IsAssignableFrom(x.ClrType))
            .ToList();

        Assert.NotEmpty(entityTypes);
        Assert.All(entityTypes, entity => Assert.NotEmpty(entity.GetDeclaredQueryFilters()));
    }

    [Fact]
    public void Model_ConfiguresBookingRelationshipsExplicitly()
    {
        using var context = CreateContext();

        var bookingDate = context.Model.FindEntityType(typeof(BookingDate));
        var bookingDeposit = context.Model.FindEntityType(typeof(BookingDeposit));
        var outboundMessage = context.Model.FindEntityType(typeof(OutboundMessage));

        Assert.Equal(DeleteBehavior.Cascade, Assert.Single(bookingDate!.GetForeignKeys()).DeleteBehavior);
        Assert.Contains(bookingDeposit!.GetForeignKeys(), x =>
            x.PrincipalEntityType.ClrType == typeof(Booking) && x.DeleteBehavior == DeleteBehavior.Cascade);
        Assert.All(outboundMessage!.GetForeignKeys(), x => Assert.Equal(DeleteBehavior.NoAction, x.DeleteBehavior));
    }
}
