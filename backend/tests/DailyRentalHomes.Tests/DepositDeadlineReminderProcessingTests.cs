using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Deposits;
using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Tests;

public sealed class DepositDeadlineReminderProcessingTests
{
    [Fact]
    public async Task QueuesReminderWhenEligibleDeadlineIsInsideWindow()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, deadline: DateTime.UtcNow.AddHours(6));
        var processor = Processor(context);

        var result = await processor.ProcessAsync(default);

        Assert.Equal(1, result.Evaluated);
        Assert.Equal(1, result.Eligible);
        Assert.Equal(1, result.Queued);
        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(NotificationTypeCodes.DepositDeadlineReminder, message.TypeCode);
        Assert.Equal(30, message.RecipientUserId);
        Assert.Equal(501, message.BookingDepositId);
    }

    [Fact]
    public async Task DoesNotQueueReminderWhenDeadlineIsOutsideWindow()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, deadline: DateTime.UtcNow.AddHours(30));
        var processor = Processor(context);

        var result = await processor.ProcessAsync(default);

        Assert.Equal(1, result.Evaluated);
        Assert.Equal(0, result.Eligible);
        Assert.Equal(0, result.Queued);
        Assert.Empty(context.OutboundMessages);
    }

    [Fact]
    public async Task DoesNotQueueForApprovedDeposit()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, deadline: DateTime.UtcNow.AddHours(6), depositStatus: BookingDepositStatus.Paid);
        var processor = Processor(context);

        var result = await processor.ProcessAsync(default);

        Assert.Equal(0, result.Eligible);
        Assert.Empty(context.OutboundMessages);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public async Task DoesNotQueueForCancelledCompletedOrRejectedBookings(long statusId)
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, deadline: DateTime.UtcNow.AddHours(6), bookingStatusId: statusId);
        var processor = Processor(context);

        var result = await processor.ProcessAsync(default);

        Assert.Equal(0, result.Eligible);
        Assert.Empty(context.OutboundMessages);
    }

    [Fact]
    public async Task RepeatedProcessingDoesNotQueueDuplicateForSameDeadline()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, deadline: DateTime.UtcNow.AddHours(6));
        var processor = Processor(context);

        var first = await processor.ProcessAsync(default);
        var second = await processor.ProcessAsync(default);

        Assert.Equal(1, first.Queued);
        Assert.Equal(0, second.Queued);
        Assert.Equal(1, second.DuplicateSkipped);
        Assert.Equal(1, await context.OutboundMessages.CountAsync());
    }

    [Fact]
    public async Task ExtendingDeadlineAllowsNewReminderCycle()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, deadline: DateTime.UtcNow.AddHours(6));
        var processor = Processor(context);
        await processor.ProcessAsync(default);

        var deposit = await context.BookingDeposits.SingleAsync();
        deposit.DeadlineAt = DateTime.UtcNow.AddHours(8);
        deposit.DeadlineExtendedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var result = await processor.ProcessAsync(default);

        Assert.Equal(1, result.Queued);
        Assert.Equal(2, await context.OutboundMessages.CountAsync());
    }

    [Fact]
    public async Task AdminCanManuallyTriggerProcessing()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, deadline: DateTime.UtcNow.AddHours(6));
        var controller = new AdminDepositDeadlineRemindersController(Processor(context))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(1, UserRole.Admin) } }
        };

        var response = GetData<DepositDeadlineReminderProcessingResponse>(await controller.Process(default));

        Assert.Equal(1, response.Queued);
        Assert.Equal(1, await context.OutboundMessages.CountAsync());
    }

    [Theory]
    [InlineData(UserRole.Broker)]
    [InlineData(UserRole.Customer)]
    public async Task BrokerAndCustomerCannotSatisfyAdminProcessingPolicy(UserRole role)
    {
        var authorization = CreateAuthorizationService();

        var result = await authorization.AuthorizeAsync(Principal(10, role), null, AuthorizationPolicies.AdminOnly);

        Assert.False(result.Succeeded);
        var attribute = Assert.Single(typeof(AdminDepositDeadlineRemindersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.AdminOnly, attribute.Policy);
    }

    [Fact]
    public async Task ProcessingDoesNotModifyBookingOrDepositLifecycleState()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddDeposit(context, deadline: DateTime.UtcNow.AddHours(6));
        var processor = Processor(context);

        await processor.ProcessAsync(default);

        var booking = await context.Bookings.SingleAsync();
        var deposit = await context.BookingDeposits.SingleAsync();
        Assert.Equal(2, booking.StatusId);
        Assert.Equal(100, deposit.Amount);
        Assert.Equal(BookingDepositStatus.Waiting, deposit.Status);
        Assert.NotNull(deposit.DeadlineAt);
    }

    private static DepositDeadlineReminderProcessingService Processor(AppDbContext context, int reminderBeforeHours = 24) =>
        new(
            context,
            new NotificationOutboxService(context),
            Options.Create(new DepositReminderOptions { ReminderBeforeHours = reminderBeforeHours }));

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
            new BookingStatus { Id = 5, Name = "Cancelled", Code = BookingStatusCodes.Cancelled, SortOrder = 5 },
            new BookingStatus { Id = 6, Name = "Completed", Code = BookingStatusCodes.Completed, SortOrder = 6 },
            new BookingStatus { Id = 7, Name = "Rejected", Code = BookingStatusCodes.Rejected, SortOrder = 7 });
        context.Users.AddRange(
            User(10, "+994501000010", UserRole.Broker, "Broker One"),
            User(30, "+994501000030", UserRole.Customer, "Customer One"));
        context.RentalHomes.Add(Home(101, 10));
        context.Bookings.Add(Booking(1001, 101, 30, 2));
        await context.SaveChangesAsync();
    }

    private static async Task AddDeposit(
        AppDbContext context,
        DateTime deadline,
        BookingDepositStatus depositStatus = BookingDepositStatus.Waiting,
        long bookingStatusId = 2)
    {
        var booking = await context.Bookings.SingleAsync();
        booking.StatusId = bookingStatusId;
        context.BookingDeposits.Add(new BookingDeposit
        {
            Id = 501,
            BookingId = booking.Id,
            Amount = 100,
            Status = depositStatus,
            DeadlineAt = deadline,
            AllowReupload = true
        });
        await context.SaveChangesAsync();
    }

    private static IAuthorizationService CreateAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal Principal(long userId, UserRole role) => new(new ClaimsIdentity(
        [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()), new Claim(ClaimTypes.Role, role.ToString())],
        "test",
        ClaimTypes.Name,
        ClaimTypes.Role));

    private static User User(long id, string phone, UserRole role, string name) => new() { Id = id, FullName = name, PhoneNumber = phone, Role = role };
    private static RentalHome Home(long id, long brokerId) => new() { Id = id, BrokerUserId = brokerId, Title = "Home", Description = "Test", City = "Qəbələ", DailyPrice = 100, RoomCount = 2, GuestCount = 6, IsPublished = true };
    private static Booking Booking(long id, long homeId, long customerId, long statusId) => new() { Id = id, RentalHomeId = homeId, CustomerUserId = customerId, CustomerFullName = "Customer", CustomerPhoneNumber = "+994501000030", GuestCount = 2, DailyPrice = 100, TotalAmount = 100, StatusId = statusId, Dates = [new BookingDate { Date = new DateOnly(2026, 8, 10) }] };

    private static T GetData<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<T>>(ok.Value);
        Assert.True(response.Success);
        return Assert.IsAssignableFrom<T>(response.Data);
    }
}
