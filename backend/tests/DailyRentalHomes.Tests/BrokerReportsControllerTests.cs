using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Broker;
using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Tests;

public sealed class BrokerReportsControllerTests
{
    [Fact]
    public async Task BrokerSeesOnlyOwnBookingsInSummary()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateBrokerController(context, brokerId: 10);

        var response = GetData<BrokerReportSummaryResponse>(await controller.GetSummary(null, null, default));

        Assert.Equal(4, response.BookingCount);
        Assert.Equal(2, response.RevenueBookingCount);
        Assert.Equal(700m, response.TotalBookingAmount);
        Assert.Equal(355m, response.TotalExpenses);
    }

    [Fact]
    public async Task AdminCanSeeAllBookings()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, userId: 1, UserRole.Admin);

        var response = GetData<BrokerReportSummaryResponse>(await controller.GetSummary(null, null, default));

        Assert.Equal(5, response.BookingCount);
        Assert.Equal(3, response.RevenueBookingCount);
        Assert.Equal(1000m, response.TotalBookingAmount);
        Assert.Equal(425m, response.TotalExpenses);
    }

    [Fact]
    public async Task CustomerAndUnauthenticatedUsersCannotSatisfyBrokerReportPolicy()
    {
        var authorization = CreateAuthorizationService();

        var unauthenticated = await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            null,
            AuthorizationPolicies.BrokerOrAdmin);
        var customer = await authorization.AuthorizeAsync(
            Principal(30, UserRole.Customer),
            null,
            AuthorizationPolicies.BrokerOrAdmin);

        Assert.False(unauthenticated.Succeeded);
        Assert.False(customer.Succeeded);
        var attribute = Assert.Single(typeof(BrokerReportsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.BrokerOrAdmin, attribute.Policy);
    }

    [Fact]
    public async Task RejectedAndCancelledBookingsAreExcludedFromRevenueTotals()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateBrokerController(context, brokerId: 10);

        var response = GetData<BrokerReportSummaryResponse>(await controller.GetSummary(null, null, default));

        Assert.Equal(4, response.BookingCount);
        Assert.Equal(2, response.RevenueBookingCount);
        Assert.Equal(700m, response.TotalBookingAmount);
    }

    [Fact]
    public async Task ExpensesAreIncludedAndGroupedCorrectly()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateBrokerController(context, brokerId: 10);

        var response = GetData<BrokerReportSummaryResponse>(await controller.GetSummary(null, null, default));

        Assert.Equal(355m, response.TotalExpenses);
        Assert.Equal(50m, response.TotalCleaningCost);
        Assert.Equal(250m, response.TotalOwnerPayout);
        Assert.Equal(55m, response.TotalOtherExpenses);
    }

    [Fact]
    public async Task EstimatedProfitIsCalculatedFromRevenueAndExpenses()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateBrokerController(context, brokerId: 10);

        var response = GetData<BrokerReportSummaryResponse>(await controller.GetSummary(null, null, default));

        Assert.Equal(345m, response.EstimatedProfit);
    }

    [Fact]
    public async Task DateRangeIncludesBookingWithAtLeastOneDateInsideRange()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateBrokerController(context, brokerId: 10);

        var response = GetData<BrokerReportSummaryResponse>(await controller.GetSummary(
            new DateOnly(2026, 7, 11),
            new DateOnly(2026, 7, 11),
            default));

        Assert.Equal(1, response.BookingCount);
        Assert.Equal(1, response.RevenueBookingCount);
        Assert.Equal(500m, response.TotalBookingAmount);
        Assert.Equal(330m, response.TotalExpenses);
        Assert.Equal(new DateOnly(2026, 7, 11), response.From);
        Assert.Equal(new DateOnly(2026, 7, 11), response.To);
    }

    [Fact]
    public async Task InvalidDateRangeReturnsBadRequest()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateBrokerController(context, brokerId: 10);

        Assert.IsType<BadRequestObjectResult>(await controller.GetSummary(new DateOnly(2026, 7, 1), null, default));
        Assert.IsType<BadRequestObjectResult>(await controller.GetSummary(null, new DateOnly(2026, 7, 31), default));
        Assert.IsType<BadRequestObjectResult>(await controller.GetSummary(new DateOnly(2026, 8, 1), new DateOnly(2026, 7, 31), default));
    }

    [Fact]
    public async Task SoftDeletedBookingsHomesDatesAndExpensesAreIgnored()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var deletedBooking = await context.Bookings.SingleAsync(booking => booking.Id == 1002);
        deletedBooking.IsDeleted = true;
        var deletedHome = await context.RentalHomes.SingleAsync(home => home.Id == 102);
        deletedHome.IsDeleted = true;
        var deletedDateBooking = Booking(2001, 101, 1, 999m, new DateOnly(2026, 7, 20));
        deletedDateBooking.Dates.Single().IsDeleted = true;
        context.Bookings.Add(deletedDateBooking);
        var deletedExpense = await context.BookingExpenses.SingleAsync(expense => expense.Id == 503);
        deletedExpense.IsDeleted = true;
        await context.SaveChangesAsync();
        var admin = CreateController(context, userId: 1, UserRole.Admin);

        var response = GetData<BrokerReportSummaryResponse>(await admin.GetSummary(
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 20),
            default));

        Assert.Equal(3, response.BookingCount);
        Assert.Equal(1, response.RevenueBookingCount);
        Assert.Equal(500m, response.TotalBookingAmount);
        Assert.Equal(300m, response.TotalExpenses);
        Assert.Equal(50m, response.TotalCleaningCost);
        Assert.Equal(250m, response.TotalOwnerPayout);
        Assert.Equal(0m, response.TotalOtherExpenses);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedData(AppDbContext context)
    {
        context.BookingStatuses.AddRange(
            new BookingStatus { Id = 1, Name = "Pending", Code = BookingStatusCodes.Pending, SortOrder = 1 },
            new BookingStatus { Id = 2, Name = "Waiting deposit", Code = BookingStatusCodes.WaitingDeposit, SortOrder = 2 },
            new BookingStatus { Id = 3, Name = "Paid", Code = BookingStatusCodes.Paid, SortOrder = 3 },
            new BookingStatus { Id = 4, Name = "Confirmed", Code = BookingStatusCodes.Confirmed, SortOrder = 4 },
            new BookingStatus { Id = 5, Name = "Cancelled", Code = BookingStatusCodes.Cancelled, SortOrder = 5 },
            new BookingStatus { Id = 6, Name = "Rejected", Code = BookingStatusCodes.Rejected, SortOrder = 6 },
            new BookingStatus { Id = 7, Name = "Completed", Code = BookingStatusCodes.Completed, SortOrder = 7 });
        context.Users.AddRange(
            new User { Id = 1, FullName = "Admin", PhoneNumber = "+994501000001", Role = UserRole.Admin },
            new User { Id = 10, FullName = "Broker One", PhoneNumber = "+994501000010", Role = UserRole.Broker },
            new User { Id = 20, FullName = "Broker Two", PhoneNumber = "+994501000020", Role = UserRole.Broker },
            new User { Id = 30, FullName = "Customer", PhoneNumber = "+994501000030", Role = UserRole.Customer });
        context.RentalHomes.AddRange(
            Home(101, 10, "Broker One Home"),
            Home(102, 20, "Broker Two Home"));
        context.Bookings.AddRange(
            Booking(1001, 101, 4, 500m, new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 11)),
            Booking(1002, 101, 1, 200m, new DateOnly(2026, 8, 1)),
            Booking(1003, 102, 4, 300m, new DateOnly(2026, 7, 12)),
            Booking(1004, 101, 6, 400m, new DateOnly(2026, 7, 13)),
            Booking(1005, 101, 5, 450m, new DateOnly(2026, 7, 14)));
        context.BookingExpenses.AddRange(
            Expense(501, 1001, "cleaning", 50m),
            Expense(502, 1001, "owner_payout", 250m),
            Expense(503, 1001, "utility", 30m),
            Expense(504, 1002, "repair", 25m),
            Expense(505, 1003, "cleaning", 70m));
        await context.SaveChangesAsync();
    }

    private static RentalHome Home(long id, long brokerId, string title) => new()
    {
        Id = id,
        BrokerUserId = brokerId,
        Title = title,
        Description = "Test home",
        City = "Qəbələ",
        DailyPrice = 120m,
        RoomCount = 3,
        GuestCount = 6,
        IsPublished = true
    };

    private static Booking Booking(long id, long homeId, long statusId, decimal totalAmount, params DateOnly[] dates) => new()
    {
        Id = id,
        RentalHomeId = homeId,
        CustomerFullName = "Test Customer",
        CustomerPhoneNumber = "+994501234567",
        GuestCount = 2,
        DailyPrice = 120m,
        TotalAmount = totalAmount,
        StatusId = statusId,
        Dates = dates.Select(date => new BookingDate { Date = date }).ToList()
    };

    private static BookingExpense Expense(long id, long bookingId, string typeCode, decimal amount) => new()
    {
        Id = id,
        BookingId = bookingId,
        TypeCode = typeCode,
        Title = typeCode,
        Amount = amount
    };

    private static BrokerReportsController CreateBrokerController(AppDbContext context, long brokerId) =>
        CreateController(context, brokerId, UserRole.Broker);

    private static BrokerReportsController CreateController(AppDbContext context, long userId, UserRole role)
    {
        return new BrokerReportsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = Principal(userId, role) }
            }
        };
    }

    private static ClaimsPrincipal Principal(long userId, UserRole role) =>
        new(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        ], "test", ClaimTypes.Name, ClaimTypes.Role));

    private static IAuthorizationService CreateAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static T GetData<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<T>>(ok.Value);
        Assert.True(response.Success);
        return Assert.IsAssignableFrom<T>(response.Data);
    }
}
