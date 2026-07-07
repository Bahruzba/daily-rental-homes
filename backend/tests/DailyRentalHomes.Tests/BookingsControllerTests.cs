using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Bookings;
using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Tests;

public sealed class BookingsControllerTests
{
    [Fact]
    public async Task ValidBookingCreatesBookingAndSortedDates()
    {
        await using var context = CreateContext();
        await SeedBaseData(context);
        var controller = Controller(context);

        var result = await controller.Create(Request(1, new DateOnly(2026, 7, 14), new DateOnly(2026, 7, 12)), default);

        var response = GetCreatedResponse(result);
        var booking = await context.Bookings.Include(item => item.Dates).SingleAsync();
        Assert.Equal(booking.Id, response.BookingId);
        Assert.Equal([new DateOnly(2026, 7, 12), new DateOnly(2026, 7, 14)], booking.Dates.OrderBy(item => item.Date).Select(item => item.Date));
        var notification = await context.OutboundMessages.SingleAsync();
        Assert.Equal(NotificationTypeCodes.BookingCreated, notification.TypeCode);
        Assert.Equal(100, notification.RecipientUserId);
    }

    [Fact]
    public async Task BackendCalculatesTotalFromRentalHomePrice()
    {
        await using var context = CreateContext();
        await SeedBaseData(context, dailyPrice: 125m);
        var controller = Controller(context);

        var result = await controller.Create(Request(1, new DateOnly(2026, 7, 12), new DateOnly(2026, 7, 13), new DateOnly(2026, 7, 14)), default);

        var response = GetCreatedResponse(result);
        Assert.Equal(125m, response.DailyPrice);
        Assert.Equal(375m, response.TotalAmount);
        Assert.Equal(375m, (await context.Bookings.SingleAsync()).TotalAmount);
    }

    [Fact]
    public async Task DefaultStatusIsResolvedByPendingCode()
    {
        await using var context = CreateContext();
        await SeedBaseData(context, pendingStatusId: 42);
        var controller = Controller(context);

        var result = await controller.Create(Request(1, new DateOnly(2026, 7, 12)), default);

        var response = GetCreatedResponse(result);
        Assert.Equal(42, (await context.Bookings.SingleAsync()).StatusId);
        Assert.Equal(BookingStatusCodes.Pending, response.StatusCode);
        Assert.Equal("Pending", response.StatusName);
    }

    [Fact]
    public async Task ConflictingDateForSameRentalHomeFails()
    {
        await using var context = CreateContext();
        await SeedBaseData(context);
        await AddExistingBooking(context, rentalHomeId: 1, new DateOnly(2026, 7, 11));
        var controller = Controller(context);

        var result = await controller.Create(Request(1, new DateOnly(2026, 7, 11)), default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.Contains("date conflict", response.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ManualAvailabilityBlockDateFails()
    {
        await using var context = CreateContext();
        await SeedBaseData(context);
        context.RentalHomeAvailabilityBlocks.Add(new RentalHomeAvailabilityBlock
        {
            RentalHomeId = 1,
            StartDate = new DateOnly(2026, 7, 10),
            EndDate = new DateOnly(2026, 7, 12),
            Note = "Broker private note"
        });
        await context.SaveChangesAsync();
        var controller = Controller(context);

        var result = await controller.Create(Request(1, new DateOnly(2026, 7, 11)), default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.Contains("date conflict", response.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-07-11", response.Error);
    }

    [Fact]
    public async Task ExistingConfirmedBookingDateFails()
    {
        await using var context = CreateContext();
        await SeedBaseData(context);
        var confirmed = new BookingStatus { Id = 44, Name = "Confirmed", Code = BookingStatusCodes.Confirmed, SortOrder = 2 };
        context.BookingStatuses.Add(confirmed);
        await context.SaveChangesAsync();
        await AddExistingBooking(context, rentalHomeId: 1, new DateOnly(2026, 7, 11), confirmed.Id);
        var controller = Controller(context);

        var result = await controller.Create(Request(1, new DateOnly(2026, 7, 11)), default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.Contains("date conflict", response.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FreeDateSucceedsWhenOtherDatesAreBlocked()
    {
        await using var context = CreateContext();
        await SeedBaseData(context);
        context.RentalHomeAvailabilityBlocks.Add(new RentalHomeAvailabilityBlock
        {
            RentalHomeId = 1,
            StartDate = new DateOnly(2026, 7, 10),
            EndDate = new DateOnly(2026, 7, 12)
        });
        await AddExistingBooking(context, rentalHomeId: 1, new DateOnly(2026, 7, 20));
        var controller = Controller(context);

        var result = await controller.Create(Request(1, new DateOnly(2026, 7, 15)), default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, await context.Bookings.CountAsync());
    }

    [Fact]
    public async Task SameDateForDifferentRentalHomeSucceeds()
    {
        await using var context = CreateContext();
        await SeedBaseData(context);
        await AddExistingBooking(context, rentalHomeId: 1, new DateOnly(2026, 7, 11));
        var controller = Controller(context);

        var result = await controller.Create(Request(2, new DateOnly(2026, 7, 11)), default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2, await context.Bookings.CountAsync());
    }

    [Fact]
    public async Task EmptyDatesRequestFails()
    {
        await using var context = CreateContext();
        await SeedBaseData(context);
        var controller = Controller(context);

        var result = await controller.Create(Request(1), default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.Contains("booking date", response.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DuplicateDatesInRequestFail()
    {
        await using var context = CreateContext();
        await SeedBaseData(context);
        var controller = Controller(context);
        var date = new DateOnly(2026, 7, 12);

        var result = await controller.Create(Request(1, date, date), default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.Contains("Duplicate", response.Error);
    }

    [Fact]
    public async Task CancelledBookingDoesNotBlockDate()
    {
        await using var context = CreateContext();
        await SeedBaseData(context);
        var cancelled = new BookingStatus { Id = 43, Name = "Cancelled", Code = BookingStatusCodes.Cancelled, SortOrder = 2 };
        context.BookingStatuses.Add(cancelled);
        await context.SaveChangesAsync();
        await AddExistingBooking(context, rentalHomeId: 1, new DateOnly(2026, 7, 11), cancelled.Id);
        var controller = Controller(context);

        var result = await controller.Create(Request(1, new DateOnly(2026, 7, 11)), default);

        Assert.IsType<OkObjectResult>(result);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static BookingsController Controller(AppDbContext context) =>
        new(context, new NotificationOutboxService(context));

    private static async Task SeedBaseData(AppDbContext context, decimal dailyPrice = 100m, long pendingStatusId = 41)
    {
        context.BookingStatuses.Add(new BookingStatus
        {
            Id = pendingStatusId,
            Name = "Pending",
            Code = BookingStatusCodes.Pending,
            SortOrder = 1
        });
        context.Users.Add(new User
        {
            Id = 100,
            FullName = "Test Broker",
            PhoneNumber = "+994501000100",
            Role = DailyRentalHomes.Domain.Enums.UserRole.Broker
        });
        context.RentalHomes.AddRange(
            Home(1, dailyPrice),
            Home(2, dailyPrice));
        await context.SaveChangesAsync();
    }

    private static RentalHome Home(long id, decimal dailyPrice) => new()
    {
        Id = id,
        BrokerUserId = 100,
        Title = $"Home {id}",
        Description = "Test home",
        City = "Qəbələ",
        DailyPrice = dailyPrice,
        RoomCount = 2,
        GuestCount = 6,
        IsPublished = true
    };

    private static NewBookingRequest Request(long rentalHomeId, params DateOnly[] dates) => new()
    {
        RentalHomeId = rentalHomeId,
        Name = "Test Customer",
        Phone = "+994501234567",
        Guests = 2,
        Dates = dates.ToList(),
        Note = "Test note"
    };

    private static async Task AddExistingBooking(AppDbContext context, long rentalHomeId, DateOnly date, long statusId = 41)
    {
        context.Bookings.Add(new Booking
        {
            RentalHomeId = rentalHomeId,
            CustomerFullName = "Existing Customer",
            CustomerPhoneNumber = "+994501111111",
            GuestCount = 2,
            DailyPrice = 100m,
            TotalAmount = 100m,
            StatusId = statusId,
            Dates = [new BookingDate { Date = date }]
        });
        await context.SaveChangesAsync();
    }

    private static BookingCreatedResponse GetCreatedResponse(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BookingCreatedResponse>>(ok.Value);
        Assert.True(apiResponse.Success);
        return Assert.IsType<BookingCreatedResponse>(apiResponse.Data);
    }
}
