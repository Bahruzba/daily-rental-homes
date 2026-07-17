using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Account;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Tests;

public sealed class BookingCancellationRequestControllerTests
{
    [Fact]
    public async Task CustomerCanCreateCancellationRequestForOwnActiveBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = AccountController(context, 30);

        var response = GetData<BookingCancellationRequestResponse>(await controller.RequestCancellation(
            1001,
            new CreateBookingCancellationRequest { Reason = "Plan changed" },
            default));

        Assert.Equal(1001, response.BookingId);
        Assert.Equal("pending", response.StatusCode);
        Assert.Equal("Plan changed", response.Reason);
        var request = await context.BookingCancellationRequests.SingleAsync();
        Assert.Equal(30, request.RequestedByUserId);
        Assert.Equal(30, request.CreatedByUserId);
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item =>
            item.TypeCode == NotificationTypeCodes.BookingCancellationRequested &&
            item.RecipientUserId == 10 &&
            item.BookingId == 1001);
    }

    [Fact]
    public async Task CustomerCannotCreateCancellationRequestForAnotherCustomersBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = AccountController(context, 30);

        var result = await controller.RequestCancellation(1002, new CreateBookingCancellationRequest(), default);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(context.BookingCancellationRequests);
    }

    [Theory]
    [InlineData(1005)]
    [InlineData(1006)]
    [InlineData(1007)]
    public async Task CompletedRejectedAndCancelledBookingsRejectCancellationRequest(long bookingId)
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = AccountController(context, 30);

        var result = await controller.RequestCancellation(bookingId, new CreateBookingCancellationRequest(), default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.BookingCancellationRequests);
    }

    [Fact]
    public async Task DuplicatePendingCancellationRequestReturnsBadRequest()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.BookingCancellationRequests.Add(new BookingCancellationRequest
        {
            BookingId = 1001,
            RequestedByUserId = 30,
            StatusCode = "pending"
        });
        await context.SaveChangesAsync();
        var controller = AccountController(context, 30);

        var result = await controller.RequestCancellation(1001, new CreateBookingCancellationRequest(), default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already exists", Assert.IsType<ApiResponse<object>>(badRequest.Value).Error);
        Assert.Equal(1, await context.BookingCancellationRequests.CountAsync());
    }

    [Fact]
    public async Task ReasonMaxLengthValidationReturnsBadRequest()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = AccountController(context, 30);

        var result = await controller.RequestCancellation(
            1001,
            new CreateBookingCancellationRequest { Reason = new string('x', 1001) },
            default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.BookingCancellationRequests);
    }

    [Fact]
    public async Task CancellationRequestDoesNotChangeBookingStatus()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = AccountController(context, 30);

        await controller.RequestCancellation(1001, new CreateBookingCancellationRequest(), default);

        Assert.Equal(1, (await context.Bookings.SingleAsync(item => item.Id == 1001)).StatusId);
    }

    [Fact]
    public async Task AccountDetailIncludesCancelRequestSent()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.BookingCancellationRequests.Add(new BookingCancellationRequest
        {
            BookingId = 1001,
            RequestedByUserId = 30,
            StatusCode = "pending"
        });
        await context.SaveChangesAsync();
        var controller = AccountController(context, 30);

        var response = GetData<AccountBookingDetailResponse>(await controller.GetBookingById(1001, default));

        Assert.True(response.CancelRequestSent);
    }

    [Fact]
    public async Task UnauthenticatedRequestCannotSatisfyCustomerPolicy()
    {
        var authorization = CreateAuthorizationService();

        var result = await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, AuthorizationPolicies.CustomerOnly);

        Assert.False(result.Succeeded);
        var attribute = Assert.Single(typeof(AccountController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.CustomerOnly, attribute.Policy);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static async Task SeedData(AppDbContext context)
    {
        context.BookingStatuses.AddRange(
            new BookingStatus { Id = 1, Name = "Pending", Code = BookingStatusCodes.Pending, SortOrder = 1 },
            new BookingStatus { Id = 2, Name = "WaitingDeposit", Code = BookingStatusCodes.WaitingDeposit, SortOrder = 2 },
            new BookingStatus { Id = 3, Name = "Paid", Code = BookingStatusCodes.Paid, SortOrder = 3 },
            new BookingStatus { Id = 4, Name = "Confirmed", Code = BookingStatusCodes.Confirmed, SortOrder = 4 },
            new BookingStatus { Id = 5, Name = "Completed", Code = BookingStatusCodes.Completed, SortOrder = 5 },
            new BookingStatus { Id = 6, Name = "Rejected", Code = BookingStatusCodes.Rejected, SortOrder = 6 },
            new BookingStatus { Id = 7, Name = "Cancelled", Code = BookingStatusCodes.Cancelled, SortOrder = 7 });
        context.Users.AddRange(
            User(10, "+994501000010", UserRole.Broker, "Broker One"),
            User(30, "+994501000030", UserRole.Customer, "Customer One"),
            User(31, "+994501000031", UserRole.Customer, "Customer Two"));
        context.RentalHomes.Add(Home(101, 10, "Home One"));
        context.Bookings.AddRange(
            Booking(1001, 101, 30, "+994501000030", 1),
            Booking(1002, 101, 31, "+994501000031", 1),
            Booking(1003, 101, 30, "+994501000030", 2),
            Booking(1004, 101, 30, "+994501000030", 4),
            Booking(1005, 101, 30, "+994501000030", 5),
            Booking(1006, 101, 30, "+994501000030", 6),
            Booking(1007, 101, 30, "+994501000030", 7));
        await context.SaveChangesAsync();
    }

    private static AccountController AccountController(AppDbContext context, long userId)
    {
        var environment = TestEnvironment.Create();
        return new AccountController(context, TestFileStorageFactory.Create(environment), new NotificationOutboxService(context))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(userId, UserRole.Customer) } }
        };
    }

    private static ClaimsPrincipal Principal(long userId, UserRole role) => new(new ClaimsIdentity(
        [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()), new Claim(ClaimTypes.Role, role.ToString())],
        "test", ClaimTypes.Name, ClaimTypes.Role));

    private static IAuthorizationService CreateAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static User User(long id, string phone, UserRole role, string name) => new() { Id = id, FullName = name, PhoneNumber = phone, Role = role };
    private static RentalHome Home(long id, long brokerId, string title) => new() { Id = id, BrokerUserId = brokerId, Title = title, Description = "Test", City = "Qəbələ", DailyPrice = 100, RoomCount = 2, GuestCount = 6, IsPublished = true };
    private static Booking Booking(long id, long homeId, long customerId, string phone, long statusId) => new() { Id = id, RentalHomeId = homeId, CustomerUserId = customerId, CustomerFullName = "Customer", CustomerPhoneNumber = phone, GuestCount = 2, DailyPrice = 100, TotalAmount = 100, StatusId = statusId, Dates = [new BookingDate { Date = new DateOnly(2026, 8, 10) }] };

    private static T GetData<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<T>>(ok.Value);
        Assert.True(response.Success);
        return Assert.IsAssignableFrom<T>(response.Data);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public static TestEnvironment Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "daily-rental-homes-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TestEnvironment { ContentRootPath = root, WebRootPath = Path.Combine(root, "wwwroot") };
        }

        public string ApplicationName { get; set; } = "DailyRentalHomes.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
