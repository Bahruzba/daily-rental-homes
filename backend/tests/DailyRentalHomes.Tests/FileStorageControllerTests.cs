using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Api.Storage;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Tests;

public sealed class FileStorageControllerTests
{
    [Fact]
    public async Task PropertyImageUploadUsesFileStorage()
    {
        await using var context = CreateContext();
        SeedUsersAndHome(context);
        await context.SaveChangesAsync();
        var storage = new RecordingFileStorage();
        var controller = BrokerRentalHomesController(context, storage, 10, UserRole.Broker);

        var result = await controller.UploadMedia(101, FormFile("home.webp", "image/webp"), default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(storage.SavedKeys, key => key.StartsWith("rental-homes/101/", StringComparison.Ordinal));
        var media = await context.MediaFiles.SingleAsync(item => item.RentalHomeId == 101);
        Assert.Equal(storage.SavedUrls.Single(), media.FileUrl);
    }

    [Fact]
    public async Task DepositReceiptUploadUsesFileStorage()
    {
        await using var context = CreateContext();
        SeedUsersAndHome(context);
        SeedBookingWithDeposit(context);
        await context.SaveChangesAsync();
        var storage = new RecordingFileStorage();
        var controller = AccountController(context, storage, 30);

        var result = await controller.UploadDepositReceipt(1001, FormFile("receipt.png", "image/png"), default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(storage.PrivateSavedKeys, key => key.StartsWith("deposit-receipts/", StringComparison.Ordinal));
        var receipt = await context.MediaFiles.SingleAsync(item => item.FileType == MediaFileType.DepositReceipt);
        Assert.Equal(storage.PrivateSavedUrls.Single(), receipt.FileUrl);
        Assert.False(receipt.FileUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CustomerCanDownloadOwnDepositReceipt()
    {
        await using var context = CreateContext();
        SeedUsersAndHome(context);
        SeedBookingWithDeposit(context);
        await context.SaveChangesAsync();
        var storage = new RecordingFileStorage();
        await AddUploadedReceipt(context, "deposit-receipts/receipt.png");
        await context.SaveChangesAsync();
        var controller = DepositReceiptsController(context, storage, 30, UserRole.Customer);

        var result = await controller.Download(1001, default);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal("receipt.png", file.FileDownloadName);
        Assert.Contains("deposit-receipts/receipt.png", storage.OpenedKeysOrUrls);
    }

    [Fact]
    public async Task AnotherCustomerCannotDownloadReceipt()
    {
        await using var context = CreateContext();
        SeedUsersAndHome(context);
        context.Users.Add(new User { Id = 31, FullName = "Other Customer", PhoneNumber = "+994501000031", Role = UserRole.Customer });
        SeedBookingWithDeposit(context);
        await context.SaveChangesAsync();
        await AddUploadedReceipt(context, "deposit-receipts/receipt.png");
        await context.SaveChangesAsync();
        var controller = DepositReceiptsController(context, new RecordingFileStorage(), 31, UserRole.Customer);

        var result = await controller.Download(1001, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task OwningBrokerCanDownloadReceipt()
    {
        await using var context = CreateContext();
        SeedUsersAndHome(context);
        SeedBookingWithDeposit(context);
        await context.SaveChangesAsync();
        await AddUploadedReceipt(context, "deposit-receipts/receipt.png");
        await context.SaveChangesAsync();
        var controller = DepositReceiptsController(context, new RecordingFileStorage(), 10, UserRole.Broker);

        var result = await controller.Download(1001, default);

        Assert.IsType<FileStreamResult>(result);
    }

    [Fact]
    public async Task UnrelatedBrokerCannotDownloadReceipt()
    {
        await using var context = CreateContext();
        SeedUsersAndHome(context);
        context.Users.Add(new User { Id = 20, FullName = "Other Broker", PhoneNumber = "+994501000020", Role = UserRole.Broker });
        SeedBookingWithDeposit(context);
        await context.SaveChangesAsync();
        await AddUploadedReceipt(context, "deposit-receipts/receipt.png");
        await context.SaveChangesAsync();
        var controller = DepositReceiptsController(context, new RecordingFileStorage(), 20, UserRole.Broker);

        var result = await controller.Download(1001, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AdminCanDownloadReceipt()
    {
        await using var context = CreateContext();
        SeedUsersAndHome(context);
        context.Users.Add(new User { Id = 1, FullName = "Admin", PhoneNumber = "+994501000001", Role = UserRole.Admin });
        SeedBookingWithDeposit(context);
        await context.SaveChangesAsync();
        await AddUploadedReceipt(context, "deposit-receipts/receipt.png");
        await context.SaveChangesAsync();
        var controller = DepositReceiptsController(context, new RecordingFileStorage(), 1, UserRole.Admin);

        var result = await controller.Download(1001, default);

        Assert.IsType<FileStreamResult>(result);
    }

    [Fact]
    public async Task MissingReceiptReturnsNotFound()
    {
        await using var context = CreateContext();
        SeedUsersAndHome(context);
        SeedBookingWithDeposit(context);
        await context.SaveChangesAsync();
        var controller = DepositReceiptsController(context, new RecordingFileStorage(), 30, UserRole.Customer);

        var result = await controller.Download(1001, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void DepositReceiptDownloadRequiresAuthentication()
    {
        var attribute = Assert.Single(typeof(DepositReceiptsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());
        Assert.Null(attribute.Policy);
    }

    [Fact]
    public async Task DeleteMediaRemovesStoredFileThroughAbstraction()
    {
        await using var context = CreateContext();
        SeedUsersAndHome(context);
        context.MediaFiles.Add(new MediaFile
        {
            Id = 201,
            RentalHomeId = 101,
            FileType = MediaFileType.HomeImage,
            FileName = "home.webp",
            FileUrl = "/uploads/rental-homes/101/home.webp",
            SortOrder = 0
        });
        await context.SaveChangesAsync();
        var storage = new RecordingFileStorage();
        var controller = BrokerRentalHomesController(context, storage, 10, UserRole.Broker);

        var result = await controller.DeleteMedia(101, 201, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Contains("/uploads/rental-homes/101/home.webp", storage.DeletedKeysOrUrls);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedUsersAndHome(AppDbContext context)
    {
        context.Users.AddRange(
            new User { Id = 10, FullName = "Broker", PhoneNumber = "+994501000010", Role = UserRole.Broker },
            new User { Id = 30, FullName = "Customer", PhoneNumber = "+994501000030", Role = UserRole.Customer });
        context.RentalHomes.Add(new RentalHome
        {
            Id = 101,
            BrokerUserId = 10,
            Title = "Home",
            Description = "Test",
            City = "Qəbələ",
            Address = "Test address",
            DailyPrice = 100,
            RoomCount = 2,
            GuestCount = 4,
            IsPublished = true
        });
    }

    private static void SeedBookingWithDeposit(AppDbContext context)
    {
        context.BookingStatuses.Add(new BookingStatus { Id = 1, Name = "Waiting deposit", Code = BookingStatusCodes.WaitingDeposit, SortOrder = 1 });
        context.Bookings.Add(new Booking
        {
            Id = 1001,
            RentalHomeId = 101,
            CustomerUserId = 30,
            CustomerFullName = "Customer",
            CustomerPhoneNumber = "+994501000030",
            GuestCount = 2,
            DailyPrice = 100,
            TotalAmount = 100,
            StatusId = 1,
            Dates = [new BookingDate { Date = new DateOnly(2026, 8, 10) }],
            Deposit = new BookingDeposit
            {
                Id = 501,
                Amount = 50,
                DeadlineAt = DateTime.UtcNow.AddDays(1),
                Status = BookingDepositStatus.Waiting,
                AllowReupload = true
            }
        });
    }

    private static async Task AddUploadedReceipt(AppDbContext context, string fileUrl)
    {
        var deposit = await context.BookingDeposits.Include(item => item.ReceiptFiles).SingleAsync();
        deposit.Status = BookingDepositStatus.ReceiptUploaded;
        deposit.UploadedAt = DateTime.UtcNow;
        deposit.ReceiptFiles.Add(new MediaFile
        {
            FileType = MediaFileType.DepositReceipt,
            FileName = "receipt.png",
            FileUrl = fileUrl,
            ContentType = "image/png",
            SizeBytes = 4
        });
    }

    private static BrokerRentalHomesController BrokerRentalHomesController(AppDbContext context, IFileStorage storage, long userId, UserRole role) => new(context, storage)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(userId, role) } }
    };

    private static AccountController AccountController(AppDbContext context, IFileStorage storage, long userId) => new(context, storage, new NotificationOutboxService(context))
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(userId, UserRole.Customer) } }
    };

    private static DepositReceiptsController DepositReceiptsController(AppDbContext context, IFileStorage storage, long userId, UserRole role) => new(context, storage)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(userId, role) } }
    };

    private static IFormFile FormFile(string fileName, string contentType)
    {
        var stream = new MemoryStream([1, 2, 3, 4]);
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static ClaimsPrincipal Principal(long userId, UserRole role) =>
        new(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        ], "test", ClaimTypes.Name, ClaimTypes.Role));

    private sealed class RecordingFileStorage : IFileStorage
    {
        public List<string> SavedKeys { get; } = [];
        public List<string> SavedUrls { get; } = [];
        public List<string> PrivateSavedKeys { get; } = [];
        public List<string> PrivateSavedUrls { get; } = [];
        public List<string> DeletedKeysOrUrls { get; } = [];
        public List<string> OpenedKeysOrUrls { get; } = [];
        public List<string?> ContentTypes { get; } = [];

        public Task<StoredFile> SaveAsync(string key, Stream content, string? contentType, CancellationToken cancellationToken)
        {
            SavedKeys.Add(key);
            ContentTypes.Add(contentType);
            var url = GetPublicUrl(key);
            SavedUrls.Add(url);
            return Task.FromResult(new StoredFile(key, url));
        }

        public Task<StoredFile> SavePrivateAsync(string key, Stream content, string? contentType, CancellationToken cancellationToken)
        {
            PrivateSavedKeys.Add(key);
            ContentTypes.Add(contentType);
            PrivateSavedUrls.Add(key);
            return Task.FromResult(new StoredFile(key, key));
        }

        public Task<StoredFileReadResult?> OpenReadAsync(string keyOrUrl, CancellationToken cancellationToken)
        {
            OpenedKeysOrUrls.Add(keyOrUrl);
            return Task.FromResult<StoredFileReadResult?>(new StoredFileReadResult(new MemoryStream([1, 2, 3, 4]), keyOrUrl));
        }

        public Task DeleteAsync(string keyOrUrl, CancellationToken cancellationToken)
        {
            DeletedKeysOrUrls.Add(keyOrUrl);
            return Task.CompletedTask;
        }

        public string GetPublicUrl(string key) => $"/uploads/{key.TrimStart('/')}";
    }
}
