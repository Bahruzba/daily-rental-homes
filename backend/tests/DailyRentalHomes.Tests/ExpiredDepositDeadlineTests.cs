using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Broker;
using DailyRentalHomes.Api.Contracts.Deposits;
using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Tests;

public sealed class ExpiredDepositDeadlineTests
{
    [Fact]
    public async Task PastNonApprovedDepositIsMarkedExpiredInBrokerDetail()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, 1001, DateTime.UtcNow.AddHours(-2));
        var controller = BrokerController(context, 10);

        var detail = GetData<BrokerBookingDetailResponse>(await controller.GetBookingById(1001, default));

        Assert.NotNull(detail.Deposit);
        Assert.True(detail.Deposit.IsDeadlineExpired);
    }

    [Fact]
    public async Task FutureDeadlineIsNotExpired()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, 1001, DateTime.UtcNow.AddHours(2));
        var controller = BrokerController(context, 10);

        var detail = GetData<BrokerBookingDetailResponse>(await controller.GetBookingById(1001, default));

        Assert.NotNull(detail.Deposit);
        Assert.False(detail.Deposit.IsDeadlineExpired);
    }

    [Fact]
    public async Task ApprovedDepositIsNotExpired()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, 1001, DateTime.UtcNow.AddHours(-2), BookingDepositStatus.Paid);
        var controller = BrokerController(context, 10);

        var detail = GetData<BrokerBookingDetailResponse>(await controller.GetBookingById(1001, default));

        Assert.NotNull(detail.Deposit);
        Assert.False(detail.Deposit.IsDeadlineExpired);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public async Task InactiveBookingsAreNotTreatedAsExpired(long statusId)
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, 1001, DateTime.UtcNow.AddHours(-2), bookingStatusId: statusId);
        var controller = BrokerController(context, 10);

        var detail = GetData<BrokerBookingDetailResponse>(await controller.GetBookingById(1001, default));

        Assert.NotNull(detail.Deposit);
        Assert.False(detail.Deposit.IsDeadlineExpired);
    }

    [Fact]
    public async Task CustomerDetailReturnsExpiredDeadlineField()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, 1001, DateTime.UtcNow.AddHours(-2));
        var controller = AccountController(context, 30);

        var detail = GetData<DailyRentalHomes.Api.Contracts.Account.AccountBookingDetailResponse>(
            await controller.GetBookingById(1001, default));

        Assert.NotNull(detail.Deposit);
        Assert.True(detail.Deposit.IsDeadlineExpired);
    }

    [Fact]
    public async Task BrokerExpiredDeadlineFilterReturnsMatchingOwnBookings()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, 1001, DateTime.UtcNow.AddHours(-2));
        await AddDeposit(context, 1002, DateTime.UtcNow.AddHours(2));
        var controller = BrokerController(context, 10);

        var items = GetData<IReadOnlyList<BrokerBookingListItemResponse>>(
            await controller.GetBookings(null, null, null, true, default));

        var booking = Assert.Single(items);
        Assert.Equal(1001, booking.BookingId);
        Assert.True(booking.IsDeadlineExpired);
    }

    [Fact]
    public async Task BrokerExpiredDeadlineFilterDoesNotExposeAnotherBrokersBookings()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, 1003, DateTime.UtcNow.AddHours(-2));
        var controller = BrokerController(context, 10);

        var items = GetData<IReadOnlyList<BrokerBookingListItemResponse>>(
            await controller.GetBookings(null, null, null, true, default));

        Assert.Empty(items);
    }

    [Fact]
    public async Task AdminExpiredEndpointReturnsEligibleExpiredDeposits()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, 1001, DateTime.UtcNow.AddHours(-2));
        await AddDeposit(context, 1002, DateTime.UtcNow.AddHours(2));
        await AddDeposit(context, 1003, DateTime.UtcNow.AddHours(-3));
        var controller = AdminController(context);

        var items = GetData<IReadOnlyList<ExpiredDepositDeadlineResponse>>(await controller.GetExpired(default));

        Assert.Equal([1003L, 1001L], items.Select(item => item.BookingId).ToArray());
        Assert.All(items, item => Assert.Equal(DepositStatusCodes.Requested, item.DepositStatus));
        Assert.Contains(items, item => item.RentalHomeTitle == "Broker Two Home");
    }

    [Theory]
    [InlineData(UserRole.Broker)]
    [InlineData(UserRole.Customer)]
    public async Task BrokerAndCustomerCannotAccessAdminExpiredEndpoint(UserRole role)
    {
        var authorization = CreateAuthorizationService();

        var result = await authorization.AuthorizeAsync(Principal(10, role), null, AuthorizationPolicies.AdminOnly);

        Assert.False(result.Succeeded);
        var attribute = Assert.Single(typeof(AdminDepositDeadlinesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.AdminOnly, attribute.Policy);
    }

    [Fact]
    public async Task ReadingExpiredStateDoesNotModifyLifecycleState()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, 1001, DateTime.UtcNow.AddHours(-2));
        var broker = BrokerController(context, 10);
        var admin = AdminController(context);

        await broker.GetBookingById(1001, default);
        await admin.GetExpired(default);

        var booking = await context.Bookings.SingleAsync(item => item.Id == 1001);
        var deposit = await context.BookingDeposits.SingleAsync(item => item.BookingId == 1001);
        Assert.Equal(2, booking.StatusId);
        Assert.Equal(BookingDepositStatus.Waiting, deposit.Status);
        Assert.Equal(100, deposit.Amount);
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
            new BookingStatus { Id = 2, Name = "WaitingDeposit", Code = BookingStatusCodes.WaitingDeposit, SortOrder = 2 },
            new BookingStatus { Id = 3, Name = "Confirmed", Code = BookingStatusCodes.Confirmed, SortOrder = 3 },
            new BookingStatus { Id = 4, Name = "Paid", Code = BookingStatusCodes.Paid, SortOrder = 4 },
            new BookingStatus { Id = 5, Name = "Cancelled", Code = BookingStatusCodes.Cancelled, SortOrder = 5 },
            new BookingStatus { Id = 6, Name = "Completed", Code = BookingStatusCodes.Completed, SortOrder = 6 },
            new BookingStatus { Id = 7, Name = "Rejected", Code = BookingStatusCodes.Rejected, SortOrder = 7 });
        context.Users.AddRange(
            new User { Id = 10, FullName = "Broker One", PhoneNumber = "+994501000010", Role = UserRole.Broker },
            new User { Id = 20, FullName = "Broker Two", PhoneNumber = "+994501000020", Role = UserRole.Broker },
            new User { Id = 30, FullName = "Customer One", PhoneNumber = "+994501000030", Role = UserRole.Customer });
        context.RentalHomes.AddRange(
            Home(101, 10, "Broker One Home"),
            Home(102, 20, "Broker Two Home"));
        context.Bookings.AddRange(
            Booking(1001, 101, 30, 2),
            Booking(1002, 101, 30, 2),
            Booking(1003, 102, 30, 2));
        await context.SaveChangesAsync();
    }

    private static async Task AddDeposit(
        AppDbContext context,
        long bookingId,
        DateTime deadlineAt,
        BookingDepositStatus status = BookingDepositStatus.Waiting,
        long? bookingStatusId = null)
    {
        var booking = await context.Bookings.SingleAsync(item => item.Id == bookingId);
        if (bookingStatusId.HasValue) booking.StatusId = bookingStatusId.Value;
        context.BookingDeposits.Add(new BookingDeposit
        {
            Id = 5000 + bookingId,
            BookingId = bookingId,
            Amount = 100,
            Status = status,
            DeadlineAt = deadlineAt,
            AllowReupload = true
        });
        await context.SaveChangesAsync();
    }

    private static BrokerController BrokerController(AppDbContext context, long brokerId) => new(context, new NotificationOutboxService(context))
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(brokerId, UserRole.Broker) } }
    };

    private static AdminDepositDeadlinesController AdminController(AppDbContext context) => new(context)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(1, UserRole.Admin) } }
    };

    private static AccountController AccountController(AppDbContext context, long userId)
    {
        var environment = new TestEnvironment();
        return new AccountController(context, TestFileStorageFactory.Create(environment), new NotificationOutboxService(context))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(userId, UserRole.Customer) } }
        };
    }

    private static ClaimsPrincipal Principal(long userId, UserRole role) => new(new ClaimsIdentity(
        [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()), new Claim(ClaimTypes.Role, role.ToString())],
        "test",
        ClaimTypes.Name,
        ClaimTypes.Role));

    private static IAuthorizationService CreateAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static RentalHome Home(long id, long brokerId, string title) => new() { Id = id, BrokerUserId = brokerId, Title = title, Description = "Test", City = "Qəbələ", DailyPrice = 100, RoomCount = 2, GuestCount = 6, IsPublished = true };
    private static Booking Booking(long id, long homeId, long customerId, long statusId) => new() { Id = id, RentalHomeId = homeId, CustomerUserId = customerId, CustomerFullName = "Customer", CustomerPhoneNumber = "+994501000030", GuestCount = 2, DailyPrice = 100, TotalAmount = 100, StatusId = statusId, Dates = [new BookingDate { Date = new DateOnly(2026, 8, 10) }] };

    private static T GetData<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<T>>(ok.Value);
        Assert.True(response.Success);
        return Assert.IsAssignableFrom<T>(response.Data);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "DailyRentalHomes.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
