using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Bookings;
using DailyRentalHomes.Api.Contracts.Broker;
using DailyRentalHomes.Api.Controllers;
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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Tests;

public sealed class BrokerControllerTests
{
    [Fact]
    public async Task UnauthenticatedUserCannotSatisfyBrokerPolicy()
    {
        var authorization = CreateAuthorizationService();
        var result = await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            null,
            AuthorizationPolicies.BrokerOrAdmin);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task CustomerCannotSatisfyBrokerPolicy()
    {
        var authorization = CreateAuthorizationService();
        var result = await authorization.AuthorizeAsync(
            CreatePrincipal(30, UserRole.Customer),
            null,
            AuthorizationPolicies.BrokerOrAdmin);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task BrokerCanOnlySeeOwnHomes()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.GetRentalHomes(default);
        var items = GetData<IReadOnlyList<BrokerRentalHomeResponse>>(result);

        var home = Assert.Single(items);
        Assert.Equal(101, home.Id);
        Assert.Equal(1, home.BookingCount);
    }

    [Fact]
    public async Task BrokerCanOnlySeeOwnBookings()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.GetBookings(null, null, null, default);
        var items = GetData<IReadOnlyList<BrokerBookingListItemResponse>>(result);

        var booking = Assert.Single(items);
        Assert.Equal(1001, booking.BookingId);
        Assert.Equal("Broker One Home", booking.RentalHomeTitle);
    }

    [Fact]
    public async Task BrokerCannotSeeAnotherBrokersBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.GetBookingById(1002, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task BookingDetailReturnsDatesAndTotalAmount()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.GetBookingById(1001, default);
        var detail = GetData<BrokerBookingDetailResponse>(result);

        Assert.Equal(240m, detail.TotalAmount);
        Assert.Equal([new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 11)], detail.Dates);
        Assert.Equal("Test Customer", detail.Customer.FullName);
    }

    [Fact]
    public async Task BookingDetailIncludesPendingCancellationRequest()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.BookingCancellationRequests.AddRange(
            new BookingCancellationRequest
            {
                Id = 501,
                BookingId = 1001,
                RequestedByUserId = 30,
                StatusCode = "pending",
                Reason = "Customer plans changed",
                CreatedAt = new DateTime(2026, 7, 8, 12, 0, 0, DateTimeKind.Utc)
            },
            new BookingCancellationRequest
            {
                Id = 502,
                BookingId = 1001,
                RequestedByUserId = 30,
                StatusCode = "resolved",
                Reason = "Old request",
                CreatedAt = new DateTime(2026, 7, 7, 12, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.GetBookingById(1001, default);
        var detail = GetData<BrokerBookingDetailResponse>(result);

        Assert.NotNull(detail.CancellationRequest);
        Assert.Equal(501, detail.CancellationRequest.Id);
        Assert.Equal(1001, detail.CancellationRequest.BookingId);
        Assert.Equal("pending", detail.CancellationRequest.StatusCode);
        Assert.Equal("Customer plans changed", detail.CancellationRequest.Reason);
    }

    [Fact]
    public async Task OwnerBrokerCanApproveCancellationRequest()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.BookingCancellationRequests.Add(PendingCancellationRequest(501, 1001));
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var response = GetData<BrokerCancellationRequestResponse>(await controller.ApproveCancellationRequest(
            1001,
            501,
            new BrokerCancellationDecisionRequest { Note = "Approved by broker" },
            default));

        Assert.Equal("approved", response.StatusCode);
        Assert.Equal("Approved by broker", response.DecisionNote);
        Assert.NotNull(response.DecidedAt);
        var booking = await context.Bookings.SingleAsync(item => item.Id == 1001);
        Assert.Equal(4, booking.StatusId);
        var history = await context.BookingStatusHistory.SingleAsync();
        Assert.Equal(1, history.OldStatusId);
        Assert.Equal(4, history.NewStatusId);
        Assert.Equal("Approved by broker", history.Note);
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item =>
            item.TypeCode == NotificationTypeCodes.BookingCancellationApproved &&
            item.BookingId == 1001);
    }

    [Fact]
    public async Task OwnerBrokerCanRejectCancellationRequest()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.BookingCancellationRequests.Add(PendingCancellationRequest(501, 1001));
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var response = GetData<BrokerCancellationRequestResponse>(await controller.RejectCancellationRequest(
            1001,
            501,
            new BrokerCancellationDecisionRequest { Note = "Dates still reserved" },
            default));

        Assert.Equal("rejected", response.StatusCode);
        Assert.Equal("Dates still reserved", response.DecisionNote);
        Assert.Equal(1, (await context.Bookings.SingleAsync(item => item.Id == 1001)).StatusId);
        Assert.Empty(await context.BookingStatusHistory.ToListAsync());
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item =>
            item.TypeCode == NotificationTypeCodes.BookingCancellationRejected &&
            item.BookingId == 1001 &&
            item.Text.Contains("Dates still reserved"));
    }

    [Fact]
    public async Task AnotherBrokerCannotDecideCancellationRequest()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.BookingCancellationRequests.Add(PendingCancellationRequest(501, 1002));
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.ApproveCancellationRequest(1002, 501, null, default);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("pending", (await context.BookingCancellationRequests.SingleAsync(item => item.Id == 501)).StatusCode);
    }

    [Fact]
    public async Task DuplicateCancellationDecisionIsRejected()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.BookingCancellationRequests.Add(new BookingCancellationRequest
        {
            Id = 501,
            BookingId = 1001,
            RequestedByUserId = 30,
            StatusCode = "approved"
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.RejectCancellationRequest(1001, 501, null, default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CancellationDecisionNoteMaxLengthValidationReturnsBadRequest()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.BookingCancellationRequests.Add(PendingCancellationRequest(501, 1001));
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.ApproveCancellationRequest(
            1001,
            501,
            new BrokerCancellationDecisionRequest { Note = new string('x', 1001) },
            default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("pending", (await context.BookingCancellationRequests.SingleAsync(item => item.Id == 501)).StatusCode);
    }

    [Fact]
    public async Task ApprovedCancellationRequestNoLongerBlocksPublicAvailability()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.BookingCancellationRequests.Add(PendingCancellationRequest(501, 1001));
        await context.SaveChangesAsync();
        var brokerController = CreateController(context, brokerId: 10);
        await brokerController.ApproveCancellationRequest(1001, 501, null, default);
        var publicBookingsController = new BookingsController(context, new NotificationOutboxService(context));

        var result = await publicBookingsController.Create(new NewBookingRequest
        {
            RentalHomeId = 101,
            Name = "Next Customer",
            Phone = "+994501111199",
            Guests = 2,
            Dates = [new DateOnly(2026, 8, 10)]
        }, default);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AllowedStatusChangeCreatesHistory()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.ChangeBookingStatus(
            1001,
            new ChangeBrokerBookingStatusRequest { StatusCode = BookingStatusCodes.Cancelled, Note = "Customer request" },
            default);
        var response = GetData<BrokerBookingStatusChangeResponse>(result);

        Assert.Equal(BookingStatusCodes.Cancelled, response.StatusCode);
        var history = await context.BookingStatusHistory.SingleAsync();
        Assert.Equal(10, history.ChangedByUserId);
        Assert.Equal("Customer request", history.Note);
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item =>
            item.TypeCode == NotificationTypeCodes.BookingStatusChanged && item.BookingId == 1001);
    }

    [Fact]
    public async Task BrokerCanAcceptOwnPendingBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.AcceptBooking(1001, new BrokerBookingActionRequest { Note = "Accepted" }, default);
        var response = GetData<BrokerBookingStatusChangeResponse>(result);

        Assert.Equal(BookingStatusCodes.Confirmed, response.StatusCode);
        Assert.Equal(3, (await context.Bookings.FindAsync(1001L))!.StatusId);
        Assert.Equal("Accepted", (await context.BookingStatusHistory.SingleAsync()).Note);
    }

    [Fact]
    public async Task BrokerCanRejectOwnPendingBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.RejectBooking(1001, new BrokerBookingActionRequest { Note = "Unavailable" }, default);
        var response = GetData<BrokerBookingStatusChangeResponse>(result);

        Assert.Equal(BookingStatusCodes.Rejected, response.StatusCode);
        Assert.Equal(5, (await context.Bookings.FindAsync(1001L))!.StatusId);
        Assert.Equal("Unavailable", (await context.BookingStatusHistory.SingleAsync()).Note);
    }

    [Fact]
    public async Task BrokerCanCancelConfirmedBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var booking = await context.Bookings.FindAsync(1001L);
        booking!.StatusId = 3;
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.CancelBooking(1001, new BrokerBookingActionRequest { Note = "Customer cancelled" }, default);
        var response = GetData<BrokerBookingStatusChangeResponse>(result);

        Assert.Equal(BookingStatusCodes.Cancelled, response.StatusCode);
        Assert.Equal(4, (await context.Bookings.FindAsync(1001L))!.StatusId);
    }

    [Fact]
    public async Task BrokerCannotActOnAnotherBrokersBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.AcceptBooking(1002, null, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task InvalidStatusActionReturnsBadRequest()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);
        await controller.RejectBooking(1001, null, default);

        var result = await controller.AcceptBooking(1001, null, default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SoftDeletedHomeBookingCannotBeManaged()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var home = await context.RentalHomes.FindAsync(101L);
        home!.IsDeleted = true;
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.AcceptBooking(1001, null, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CancelledThroughBrokerEndpointNoLongerBlocksDate()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var brokerController = CreateController(context, brokerId: 10);
        await brokerController.ChangeBookingStatus(
            1001,
            new ChangeBrokerBookingStatusRequest { StatusCode = BookingStatusCodes.Cancelled },
            default);
        var publicBookingsController = new BookingsController(context, new NotificationOutboxService(context));

        var result = await publicBookingsController.Create(new NewBookingRequest
        {
            RentalHomeId = 101,
            Name = "Next Customer",
            Phone = "+994501111199",
            Guests = 2,
            Dates = [new DateOnly(2026, 8, 10)]
        }, default);

        Assert.IsType<OkObjectResult>(result);
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
        var pending = new BookingStatus { Id = 1, Name = "Pending", Code = BookingStatusCodes.Pending, SortOrder = 1 };
        var waiting = new BookingStatus { Id = 2, Name = "Waiting deposit", Code = BookingStatusCodes.WaitingDeposit, SortOrder = 2 };
        var confirmed = new BookingStatus { Id = 3, Name = "Confirmed", Code = BookingStatusCodes.Confirmed, SortOrder = 3 };
        var cancelled = new BookingStatus { Id = 4, Name = "Cancelled", Code = BookingStatusCodes.Cancelled, SortOrder = 4 };
        var rejected = new BookingStatus { Id = 5, Name = "Rejected", Code = BookingStatusCodes.Rejected, SortOrder = 5 };
        context.BookingStatuses.AddRange(pending, waiting, confirmed, cancelled, rejected);
        context.Users.AddRange(
            new User { Id = 10, FullName = "Broker One", PhoneNumber = "+994501000010", Role = UserRole.Broker },
            new User { Id = 20, FullName = "Broker Two", PhoneNumber = "+994501000020", Role = UserRole.Broker },
            new User { Id = 30, FullName = "Customer One", PhoneNumber = "+994501234567", Role = UserRole.Customer });
        context.RentalHomes.AddRange(
            Home(101, 10, "Broker One Home"),
            Home(102, 20, "Broker Two Home"));
        context.Bookings.AddRange(
            Booking(1001, 101, 1, "Test Customer", 120m, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 11)),
            Booking(1002, 102, 1, "Other Customer", 150m, new DateOnly(2026, 9, 2)));
        await context.SaveChangesAsync();
    }

    private static RentalHome Home(long id, long brokerId, string title) => new()
    {
        Id = id,
        BrokerUserId = brokerId,
        Title = title,
        Description = "Test home",
        City = "Qəbələ",
        District = "Vəndam",
        DailyPrice = 120m,
        RoomCount = 3,
        GuestCount = 6,
        IsPublished = true
    };

    private static Booking Booking(long id, long homeId, long statusId, string customer, decimal dailyPrice, params DateOnly[] dates) => new()
    {
        Id = id,
        RentalHomeId = homeId,
        CustomerFullName = customer,
        CustomerPhoneNumber = "+994501234567",
        GuestCount = 2,
        DailyPrice = dailyPrice,
        TotalAmount = dailyPrice * dates.Length,
        StatusId = statusId,
        CustomerNote = "Test note",
        Dates = dates.Select(date => new BookingDate { Date = date }).ToList()
    };

    private static BookingCancellationRequest PendingCancellationRequest(long id, long bookingId) => new()
    {
        Id = id,
        BookingId = bookingId,
        RequestedByUserId = 30,
        StatusCode = "pending",
        Reason = "Customer plans changed"
    };

    private static BrokerController CreateController(AppDbContext context, long brokerId)
    {
        return new BrokerController(context, new NotificationOutboxService(context))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreatePrincipal(brokerId, UserRole.Broker) }
            }
        };
    }

    private static ClaimsPrincipal CreatePrincipal(long userId, UserRole role) =>
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
