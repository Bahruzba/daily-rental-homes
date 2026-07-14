using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Account;
using DailyRentalHomes.Api.Contracts.Bookings;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Tests;

public sealed class DepositFlowControllerTests
{
    [Fact]
    public async Task BrokerCanRequestDepositForOwnBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = BrokerController(context, 10);

        var response = GetData<DepositResponse>(await controller.RequestDeposit(1001, RequestDeposit(), default));

        Assert.Equal(DepositStatusCodes.Requested, response.StatusCode);
        Assert.Equal("**** **** **** 1234", response.CardPanMasked);
        Assert.Equal(10, (await context.BookingDeposits.SingleAsync()).CreatedByUserId);
        var notifications = await context.OutboundMessages.OrderBy(item => item.ScheduledAt).ToListAsync();
        Assert.Contains(notifications, item => item.TypeCode == NotificationTypeCodes.DepositRequested && item.RecipientUserId == 30);
        Assert.Contains(notifications, item => item.TypeCode == NotificationTypeCodes.DepositDeadlineReminder && item.ScheduledAt < response.DeadlineAt);
    }

    [Fact]
    public async Task BrokerCannotRequestDepositForAnotherBrokersBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = BrokerController(context, 10);

        var result = await controller.RequestDeposit(1003, RequestDeposit(), default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CustomerCannotSatisfyBrokerDepositPolicy()
    {
        var authorization = CreateAuthorizationService();
        var result = await authorization.AuthorizeAsync(
            Principal(30, UserRole.Customer),
            null,
            AuthorizationPolicies.BrokerOrAdmin);

        Assert.False(result.Succeeded);
        var attribute = Assert.Single(typeof(BrokerDepositsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.BrokerOrAdmin, attribute.Policy);
    }

    [Fact]
    public async Task DepositRequestMovesBookingToWaitingDeposit()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = BrokerController(context, 10);

        await controller.RequestDeposit(1001, RequestDeposit(), default);

        var booking = await context.Bookings.SingleAsync(item => item.Id == 1001);
        Assert.Equal(2, booking.StatusId);
        Assert.Equal(1, await context.BookingStatusHistory.CountAsync());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    [InlineData(50, -1)]
    public async Task InvalidAmountOrDeadlineFails(decimal amount, int deadlineDays)
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = BrokerController(context, 10);
        var input = RequestDeposit();
        input.Amount = amount;
        input.DeadlineAt = DateTime.UtcNow.AddDays(deadlineDays);

        var result = await controller.RequestDeposit(1001, input, default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.BookingDeposits);
    }

    [Fact]
    public async Task CustomerCanSeeOwnBookingWithDepositInstructions()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var environment = TestEnvironment.Create();
        var controller = AccountController(context, environment, 30);

        var response = GetData<AccountBookingDetailResponse>(await controller.GetBookingById(1001, default));

        Assert.NotNull(response.Deposit);
        Assert.Equal(DepositStatusCodes.Requested, response.Deposit.StatusCode);
        Assert.Equal("Kapital Bank", response.Deposit.BankName);
        Assert.Equal("/uploads/rental-homes/home-one.webp", response.MainImageUrl);
        Assert.Equal(BookingStatusCodes.WaitingDeposit, response.StatusCode);
    }

    [Fact]
    public async Task CustomerCanListOnlyOwnBookings()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var environment = TestEnvironment.Create();
        var controller = AccountController(context, environment, 30);

        var response = GetData<IReadOnlyList<AccountBookingListItemResponse>>(await controller.GetBookings(default));

        var booking = Assert.Single(response);
        Assert.Equal(1001, booking.BookingId);
        Assert.Equal(BookingStatusCodes.WaitingDeposit, booking.StatusCode);
        Assert.NotNull(booking.Deposit);
        Assert.Equal("/uploads/rental-homes/home-one.webp", booking.MainImageUrl);
    }

    [Fact]
    public async Task CustomerCannotSeeAnotherCustomersBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var environment = TestEnvironment.Create();
        var controller = AccountController(context, environment, 30);

        var result = await controller.GetBookingById(1002, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CustomerCanUploadReceiptAndCreatesDepositMediaFile()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var environment = TestEnvironment.Create();
        var controller = AccountController(context, environment, 30);

        var response = GetData<DepositResponse>(await controller.UploadDepositReceipt(1001, ReceiptFile(), default));

        Assert.Equal(DepositStatusCodes.ReceiptUploaded, response.StatusCode);
        var media = await context.MediaFiles.SingleAsync(item => item.FileType == MediaFileType.DepositReceipt);
        Assert.Equal(MediaFileType.DepositReceipt, media.FileType);
        Assert.True(File.Exists(Path.Combine(environment.WebRootPath, media.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item => item.TypeCode == NotificationTypeCodes.DepositReceiptUploaded && item.RecipientUserId == 10);
    }

    [Fact]
    public async Task CustomerCannotUploadReceiptForAnotherCustomersBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1002);
        var environment = TestEnvironment.Create();
        var controller = AccountController(context, environment, 30);

        var result = await controller.UploadDepositReceipt(1002, ReceiptFile(), default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ReceiptUploadAllowedOnlyForValidDepositState()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddUploadedDeposit(context, 1001);
        var environment = TestEnvironment.Create();
        var controller = AccountController(context, environment, 30);

        var result = await controller.UploadDepositReceipt(1001, ReceiptFile(), default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RejectedDepositCanBeReuploadedOnlyWhenAllowed()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddUploadedDeposit(context, 1001);
        var broker = BrokerController(context, 10);
        await broker.Reject(1001, new ReviewBookingDepositInput { Note = "Unreadable", AllowReupload = true }, default);
        var environment = TestEnvironment.Create();
        var controller = AccountController(context, environment, 30);

        var response = GetData<DepositResponse>(await controller.UploadDepositReceipt(1001, ReceiptFile(), default));

        Assert.Equal(DepositStatusCodes.ReceiptUploaded, response.StatusCode);
        Assert.Null(response.ReviewNote);
    }

    [Fact]
    public async Task RejectedDepositCannotBeReuploadedWhenDisallowed()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddUploadedDeposit(context, 1001);
        var broker = BrokerController(context, 10);
        await broker.Reject(1001, new ReviewBookingDepositInput { Note = "Final rejection", AllowReupload = false }, default);
        var environment = TestEnvironment.Create();
        var controller = AccountController(context, environment, 30);

        var result = await controller.UploadDepositReceipt(1001, ReceiptFile(), default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BrokerPrivateAvailabilityNotesAreNotExposedToCustomer()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.RentalHomeAvailabilityBlocks.Add(new RentalHomeAvailabilityBlock
        {
            RentalHomeId = 101,
            StartDate = new DateOnly(2026, 8, 20),
            EndDate = new DateOnly(2026, 8, 21),
            Note = "Private broker-only maintenance note"
        });
        await context.SaveChangesAsync();
        var environment = TestEnvironment.Create();
        var controller = AccountController(context, environment, 30);

        var response = GetData<AccountBookingDetailResponse>(await controller.GetBookingById(1001, default));
        var json = JsonSerializer.Serialize(response);

        Assert.DoesNotContain("Private broker-only maintenance note", json);
    }

    [Fact]
    public async Task BrokerCanApproveUploadedReceipt()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddUploadedDeposit(context, 1001);
        var controller = BrokerController(context, 10);

        var response = GetData<DepositResponse>(await controller.Approve(1001, new ReviewBookingDepositInput { Note = "Looks good" }, default));

        Assert.Equal(DepositStatusCodes.Approved, response.StatusCode);
        Assert.Equal(3, (await context.Bookings.SingleAsync(item => item.Id == 1001)).StatusId);
        Assert.NotNull((await context.BookingDeposits.SingleAsync()).ReviewedAt);
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item => item.TypeCode == NotificationTypeCodes.DepositApproved && item.RecipientUserId == 30);
    }

    [Fact]
    public async Task BrokerCanRejectUploadedReceiptAndAllowReupload()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddUploadedDeposit(context, 1001);
        var controller = BrokerController(context, 10);

        var response = GetData<DepositResponse>(await controller.Reject(1001, new ReviewBookingDepositInput { Note = "Unreadable", AllowReupload = true }, default));

        Assert.Equal(DepositStatusCodes.Rejected, response.StatusCode);
        Assert.True(response.AllowReupload);
        Assert.Equal(2, (await context.Bookings.SingleAsync(item => item.Id == 1001)).StatusId);
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item => item.TypeCode == NotificationTypeCodes.DepositRejected && item.RecipientUserId == 30);
    }

    [Fact]
    public async Task ApprovalRequiresUploadedReceipt()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var controller = BrokerController(context, 10);

        var result = await controller.Approve(1001, new ReviewBookingDepositInput(), default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RejectedReceiptKeepsBookingDateBlocked()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddUploadedDeposit(context, 1001);
        var brokerController = BrokerController(context, 10);
        await brokerController.Reject(1001, new ReviewBookingDepositInput(), default);
        var publicController = new BookingsController(context, new NotificationOutboxService(context));

        var result = await publicController.Create(new NewBookingRequest
        {
            RentalHomeId = 101,
            Name = "Next Customer",
            Phone = "+994501111177",
            Guests = 2,
            Dates = [new DateOnly(2026, 8, 10)]
        }, default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("date conflict", Assert.IsType<ApiResponse<object>>(badRequest.Value).Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OwnerBrokerCanExtendDepositDeadline()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var controller = BrokerController(context, 10);
        var newDeadline = DateTime.UtcNow.AddDays(5);

        var response = GetData<ExtendDepositDeadlineResponse>(await controller.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = newDeadline, Reason = "Müştəri əlavə vaxt istədi" },
            default));

        Assert.Equal(1001, response.BookingId);
        Assert.Equal(501, response.DepositId);
        Assert.Equal(newDeadline, response.DeadlineAt);
        Assert.Equal("Müştəri əlavə vaxt istədi", response.DeadlineExtensionReason);
        Assert.True(response.DeadlineExtendedAt <= DateTime.UtcNow);
        var deposit = await context.BookingDeposits.SingleAsync();
        Assert.Equal(10, deposit.DeadlineExtendedByUserId);
    }

    [Fact]
    public async Task AnotherBrokerCannotExtendDepositDeadline()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1003);
        var controller = BrokerController(context, 10);

        var result = await controller.ExtendDeadline(
            1003,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddDays(5) },
            default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task MissingBookingOrDepositReturnsNotFoundWhenExtendingDeadline()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = BrokerController(context, 10);

        var missingBooking = await controller.ExtendDeadline(
            9999,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddDays(5) },
            default);
        var missingDeposit = await controller.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddDays(5) },
            default);

        Assert.IsType<NotFoundObjectResult>(missingBooking);
        Assert.IsType<NotFoundObjectResult>(missingDeposit);
    }

    [Fact]
    public async Task ExtendedDepositDeadlineMustBeInFutureAndLaterThanCurrentDeadline()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var controller = BrokerController(context, 10);
        var currentDeadline = (await context.BookingDeposits.SingleAsync()).DeadlineAt!.Value;

        var past = await controller.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddMinutes(-1) },
            default);
        var earlierThanCurrent = await controller.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = currentDeadline.AddMinutes(-1) },
            default);

        Assert.IsType<BadRequestObjectResult>(past);
        Assert.IsType<BadRequestObjectResult>(earlierThanCurrent);
    }

    [Fact]
    public async Task ApprovedDepositCannotBeExtended()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var deposit = await context.BookingDeposits.SingleAsync();
        deposit.Status = BookingDepositStatus.Paid;
        await context.SaveChangesAsync();
        var controller = BrokerController(context, 10);

        var result = await controller.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddDays(5) },
            default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task CancelledCompletedRejectedBookingsCannotExtendDepositDeadline(long statusId)
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var booking = await context.Bookings.SingleAsync(item => item.Id == 1001);
        booking.StatusId = statusId;
        await context.SaveChangesAsync();
        var controller = BrokerController(context, 10);

        var result = await controller.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddDays(5) },
            default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeadlineExtensionReasonMaxLengthValidationReturnsBadRequest()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var controller = BrokerController(context, 10);

        var result = await controller.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddDays(5), Reason = new string('x', 501) },
            default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeadlineExtensionDoesNotChangeBookingStatusOrDepositAmountOrStatus()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var controller = BrokerController(context, 10);

        await controller.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddDays(5) },
            default);

        var booking = await context.Bookings.SingleAsync(item => item.Id == 1001);
        var deposit = await context.BookingDeposits.SingleAsync();
        Assert.Equal(2, booking.StatusId);
        Assert.Equal(100, deposit.Amount);
        Assert.Equal(BookingDepositStatus.Waiting, deposit.Status);
    }

    [Fact]
    public async Task DeadlineExtensionQueuesCustomerNotification()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        context.OutboundMessages.RemoveRange(context.OutboundMessages);
        await context.SaveChangesAsync();
        var controller = BrokerController(context, 10);

        await controller.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddDays(5), Reason = "Müştəri əlavə vaxt istədi" },
            default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(NotificationTypeCodes.DepositDeadlineExtended, message.TypeCode);
        Assert.Equal("Beh müddəti uzadıldı", message.Title);
        Assert.Equal(30, message.RecipientUserId);
        Assert.Contains("Müştəri əlavə vaxt istədi", message.Text);
    }

    [Fact]
    public async Task BrokerDetailReturnsDepositDeadlineExtensionFields()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var controller = BrokerController(context, 10);
        await controller.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddDays(5), Reason = "Extra time" },
            default);
        var detailController = BrokerBookingsController(context, 10);

        var detail = GetData<BrokerBookingDetailResponse>(await detailController.GetBookingById(1001, default));

        Assert.NotNull(detail.Deposit);
        Assert.NotNull(detail.Deposit.DeadlineExtendedAt);
        Assert.Equal("Extra time", detail.Deposit.DeadlineExtensionReason);
    }

    [Fact]
    public async Task CustomerDetailReturnsDepositDeadlineExtensionFields()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddRequestedDeposit(context, 1001);
        var broker = BrokerController(context, 10);
        await broker.ExtendDeadline(
            1001,
            new ExtendDepositDeadlineRequest { DeadlineAt = DateTime.UtcNow.AddDays(5), Reason = "Extra time" },
            default);
        var environment = TestEnvironment.Create();
        var controller = AccountController(context, environment, 30);

        var detail = GetData<AccountBookingDetailResponse>(await controller.GetBookingById(1001, default));

        Assert.NotNull(detail.Deposit);
        Assert.NotNull(detail.Deposit.DeadlineExtendedAt);
        Assert.Equal("Extra time", detail.Deposit.DeadlineExtensionReason);
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
            new BookingStatus { Id = 3, Name = "Confirmed", Code = BookingStatusCodes.Confirmed, SortOrder = 3 },
            new BookingStatus { Id = 4, Name = "Cancelled", Code = BookingStatusCodes.Cancelled, SortOrder = 4 },
            new BookingStatus { Id = 5, Name = "Rejected", Code = BookingStatusCodes.Rejected, SortOrder = 5 },
            new BookingStatus { Id = 6, Name = "Completed", Code = BookingStatusCodes.Completed, SortOrder = 6 });
        context.Users.AddRange(
            User(10, "+994501000010", UserRole.Broker, "Broker One"),
            User(20, "+994501000020", UserRole.Broker, "Broker Two"),
            User(30, "+994501000030", UserRole.Customer, "Customer One"),
            User(31, "+994501000031", UserRole.Customer, "Customer Two"));
        context.RentalHomes.AddRange(Home(101, 10, "Home One"), Home(102, 20, "Home Two"));
        context.MediaFiles.Add(new MediaFile
        {
            RentalHomeId = 101,
            FileType = MediaFileType.HomeImage,
            FileName = "home-one.webp",
            FileUrl = "/uploads/rental-homes/home-one.webp",
            ContentType = "image/webp",
            SizeBytes = 100,
            SortOrder = 0
        });
        context.Bookings.AddRange(
            Booking(1001, 101, 30, "+994501000030", 1, new DateOnly(2026, 8, 10)),
            Booking(1002, 101, 31, "+994501000031", 1, new DateOnly(2026, 8, 11)),
            Booking(1003, 102, 31, "+994501000031", 1, new DateOnly(2026, 8, 12)));
        await context.SaveChangesAsync();
    }

    private static async Task AddRequestedDeposit(AppDbContext context, long bookingId)
    {
        var booking = await context.Bookings.SingleAsync(item => item.Id == bookingId);
        booking.StatusId = 2;
        context.BookingDeposits.Add(new BookingDeposit
        {
            Id = 501,
            BookingId = bookingId,
            Amount = 100,
            DeadlineAt = DateTime.UtcNow.AddDays(2),
            Status = BookingDepositStatus.Waiting,
            PaymentCard = new PaymentCard { Id = 601, BrokerUserId = 10, CardHolderName = "Broker One", PanMasked = "**** **** **** 1234", BankName = "Kapital Bank" },
            AllowReupload = true
        });
        await context.SaveChangesAsync();
    }

    private static async Task AddUploadedDeposit(AppDbContext context, long bookingId)
    {
        await AddRequestedDeposit(context, bookingId);
        var deposit = await context.BookingDeposits.Include(item => item.ReceiptFiles).SingleAsync();
        deposit.Status = BookingDepositStatus.ReceiptUploaded;
        deposit.UploadedAt = DateTime.UtcNow;
        deposit.ReceiptFiles.Add(new MediaFile { FileType = MediaFileType.DepositReceipt, FileName = "receipt.png", FileUrl = "/uploads/deposit-receipts/receipt.png", ContentType = "image/png", SizeBytes = 4 });
        await context.SaveChangesAsync();
    }

    private static BrokerDepositsController BrokerController(AppDbContext context, long userId) => new(context, new NotificationOutboxService(context))
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(userId, UserRole.Broker) } }
    };

    private static DailyRentalHomes.Api.Controllers.BrokerController BrokerBookingsController(AppDbContext context, long userId) => new(context, new NotificationOutboxService(context))
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(userId, UserRole.Broker) } }
    };

    private static AccountController AccountController(AppDbContext context, IWebHostEnvironment environment, long userId) => new(context, environment, new NotificationOutboxService(context))
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(userId, UserRole.Customer) } }
    };

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

    private static RequestBookingDepositInput RequestDeposit() => new()
    {
        Amount = 100,
        DeadlineAt = DateTime.UtcNow.AddDays(2),
        CardHolderName = "Broker One",
        CardPanMasked = "**** **** **** 1234",
        BankName = "Kapital Bank",
        Note = "Pay before deadline"
    };

    private static IFormFile ReceiptFile()
    {
        var stream = new MemoryStream([137, 80, 78, 71]);
        return new FormFile(stream, 0, stream.Length, "file", "receipt.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
    }

    private static User User(long id, string phone, UserRole role, string name) => new() { Id = id, FullName = name, PhoneNumber = phone, Role = role };
    private static RentalHome Home(long id, long brokerId, string title) => new() { Id = id, BrokerUserId = brokerId, Title = title, Description = "Test", City = "Qəbələ", DailyPrice = 100, RoomCount = 2, GuestCount = 6, IsPublished = true };
    private static Booking Booking(long id, long homeId, long customerId, string phone, long statusId, DateOnly date) => new() { Id = id, RentalHomeId = homeId, CustomerUserId = customerId, CustomerFullName = "Customer", CustomerPhoneNumber = phone, GuestCount = 2, DailyPrice = 100, TotalAmount = 100, StatusId = statusId, Dates = [new BookingDate { Date = date }] };

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
