using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Broker;
using DailyRentalHomes.Api.Contracts.RentalHomes;
using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Security;
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

public sealed class BrokerRentalHomesControllerTests
{
    [Fact]
    public async Task UnauthenticatedUserCannotCreateBrokerRentalHome()
    {
        var authorization = CreateAuthorizationService();
        var result = await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            null,
            AuthorizationPolicies.BrokerOrAdmin);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task CustomerCannotCreateBrokerRentalHome()
    {
        var authorization = CreateAuthorizationService();
        var result = await authorization.AuthorizeAsync(
            CreatePrincipal(30, UserRole.Customer),
            null,
            AuthorizationPolicies.BrokerOrAdmin);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task BrokerCanCreateOwnRentalHomeAsDraft()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.Create(ValidRequest("New broker home"), default);
        var response = GetData<BrokerRentalHomeSaveResponse>(result);

        var home = await context.RentalHomes.SingleAsync(item => item.Id == response.Id);
        Assert.Equal(10, home.BrokerUserId);
        Assert.False(home.IsPublished);
    }

    [Fact]
    public async Task BrokerCanUpdateOwnRentalHome()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.Update(101, ValidRequest("Updated title") with { DailyPrice = 175m }, default);

        Assert.IsType<OkObjectResult>(result);
        var home = await context.RentalHomes.SingleAsync(item => item.Id == 101);
        Assert.Equal("Updated title", home.Title);
        Assert.Equal(175m, home.DailyPrice);
    }

    [Fact]
    public async Task BrokerCannotUpdateAnotherBrokersRentalHome()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.Update(102, ValidRequest("Leaked update"), default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task BrokerCanPublishAndUnpublishOwnRentalHome()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        await controller.Unpublish(101, default);
        Assert.False((await context.RentalHomes.SingleAsync(item => item.Id == 101)).IsPublished);

        await controller.Publish(101, default);
        Assert.True((await context.RentalHomes.SingleAsync(item => item.Id == 101)).IsPublished);
    }

    [Fact]
    public async Task BrokerCannotPublishAnotherBrokersRentalHome()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.Publish(102, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task BrokerCanUploadImageMediaForOwnRentalHome()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.RentalHomes.Add(Home(103, 10, "No Media Home"));
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.UploadMedia(103, ImageFile("home.webp", "image/webp"), default);
        var media = GetData<BrokerRentalHomeMediaUploadResponse>(result);

        Assert.Equal("HomeImage", media.Type);
        Assert.True(media.IsMain);
        Assert.StartsWith("/uploads/rental-homes/103/", media.Url);
        Assert.Contains(await context.MediaFiles.ToListAsync(), item => item.RentalHomeId == 103 && item.FileType == MediaFileType.HomeImage);
    }

    [Fact]
    public async Task UploadRejectsUnsupportedFileType()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.UploadMedia(101, ImageFile("shell.exe", "application/octet-stream"), default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadRejectsOversizedFile()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.UploadMedia(101, new FakeFormFile("large.jpg", "image/jpeg", 5 * 1024 * 1024 + 1), default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task BrokerCannotDeleteAnotherBrokersMedia()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.DeleteMedia(102, 202, default);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.False((await context.MediaFiles.SingleAsync(item => item.Id == 202)).IsDeleted);
    }

    [Fact]
    public async Task SettingMainImageWorksForOwnHome()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.SetMainMedia(101, 201, default);
        var response = GetData<BrokerRentalHomeMediaUploadResponse>(result);

        Assert.True(response.IsMain);
        Assert.Equal(0, (await context.MediaFiles.SingleAsync(item => item.Id == 201)).SortOrder);
        Assert.Equal(1, (await context.MediaFiles.SingleAsync(item => item.Id == 200)).SortOrder);
    }

    [Fact]
    public async Task PublicListingAndDetailStillReturnRentalHomeMedia()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = new RentalHomesController(context);

        var list = GetObjectData<IReadOnlyList<RentalHomeResponse>>(await controller.GetList(default));
        var item = Assert.Single(list, home => home.Id == 101);
        Assert.Equal("/uploads/rental-homes/101/main.webp", item.MainImageUrl);

        var detail = GetObjectData<RentalHomeDetailResponse>(await controller.GetById(101, default));
        Assert.Equal("/uploads/rental-homes/101/main.webp", detail.MediaFiles[0].FileUrl);
    }

    [Fact]
    public async Task BrokerCanAddAvailabilityBlockForOwnRentalHome()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.AddAvailabilityBlock(
            101,
            new BrokerAvailabilityBlockRequest(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), "Owner stay"),
            default);
        var response = GetData<BrokerAvailabilityBlockResponse>(result);

        Assert.Equal(new DateOnly(2026, 8, 1), response.StartDate);
        Assert.Equal("Owner stay", response.Note);
        var block = await context.RentalHomeAvailabilityBlocks.SingleAsync();
        Assert.Equal(101, block.RentalHomeId);
        Assert.Equal(10, block.CreatedByUserId);
    }

    [Fact]
    public async Task BrokerCannotAddAvailabilityBlockForAnotherBrokersHome()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.AddAvailabilityBlock(
            102,
            new BrokerAvailabilityBlockRequest(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), null),
            default);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(await context.RentalHomeAvailabilityBlocks.ToListAsync());
    }

    [Fact]
    public async Task BrokerCanDeleteOwnAvailabilityBlock()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.RentalHomeAvailabilityBlocks.Add(Block(301, 101, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), "Private"));
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.DeleteAvailabilityBlock(101, 301, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.True((await context.RentalHomeAvailabilityBlocks.IgnoreQueryFilters().SingleAsync(item => item.Id == 301)).IsDeleted);
    }

    [Fact]
    public async Task PublicDetailIncludesUnavailableRangesWithoutBrokerNotes()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.RentalHomeAvailabilityBlocks.Add(Block(301, 101, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), "Private broker note"));
        await context.SaveChangesAsync();
        var controller = new RentalHomesController(context);

        var detail = GetObjectData<RentalHomeDetailResponse>(await controller.GetById(101, default));

        var range = Assert.Single(detail.UnavailableRanges);
        Assert.Equal(new DateOnly(2026, 8, 1), range.StartDate);
        Assert.Equal(new DateOnly(2026, 8, 3), range.EndDate);
        Assert.DoesNotContain(detail.GetType().GetProperties(), property => property.Name.Contains("Note", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PublicDetailIncludesExistingBookingDatesAsUnavailableRanges()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.BookingStatuses.Add(new BookingStatus { Id = 401, Name = "Confirmed", Code = DailyRentalHomes.Domain.Constants.BookingStatusCodes.Confirmed, SortOrder = 1 });
        context.Bookings.Add(new Booking
        {
            Id = 501,
            RentalHomeId = 101,
            CustomerFullName = "Existing Customer",
            CustomerPhoneNumber = "+994501111111",
            GuestCount = 2,
            DailyPrice = 120m,
            TotalAmount = 240m,
            StatusId = 401,
            Dates =
            [
                new BookingDate { Date = new DateOnly(2026, 8, 4) },
                new BookingDate { Date = new DateOnly(2026, 8, 5) }
            ]
        });
        await context.SaveChangesAsync();
        var controller = new RentalHomesController(context);

        var detail = GetObjectData<RentalHomeDetailResponse>(await controller.GetById(101, default));

        Assert.Contains(detail.UnavailableRanges, range => range.StartDate == new DateOnly(2026, 8, 4) && range.EndDate == new DateOnly(2026, 8, 4));
        Assert.Contains(detail.UnavailableRanges, range => range.StartDate == new DateOnly(2026, 8, 5) && range.EndDate == new DateOnly(2026, 8, 5));
    }

    [Fact]
    public async Task BrokerDetailIncludesAvailabilityBlocks()
    {
        await using var context = CreateContext();
        await SeedData(context);
        context.RentalHomeAvailabilityBlocks.Add(Block(301, 101, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 3), "Private"));
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var detail = GetData<BrokerRentalHomeDetailResponse>(await controller.GetById(101, default));

        var block = Assert.Single(detail.AvailabilityBlocks);
        Assert.Equal("Private", block.Note);
    }

    [Fact]
    public async Task PublicListExcludesUnpublishedHomes()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var draft = Home(103, 10, "Draft Home");
        draft.IsPublished = false;
        context.RentalHomes.Add(draft);
        await context.SaveChangesAsync();
        var controller = new RentalHomesController(context);

        var list = GetObjectData<IReadOnlyList<RentalHomeResponse>>(await controller.GetList(default));

        Assert.DoesNotContain(list, home => home.Id == 103);
        Assert.All(list, home => Assert.True(home.IsPublished));
    }

    [Fact]
    public async Task PublicDetailReturnsNotFoundForUnpublishedHome()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var draft = Home(103, 10, "Draft Home");
        draft.IsPublished = false;
        context.RentalHomes.Add(draft);
        await context.SaveChangesAsync();
        var controller = new RentalHomesController(context);

        var result = await controller.GetById(103, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task PublicListAndDetailExcludeDeletedMedia()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var deletedMedia = await context.MediaFiles.SingleAsync(item => item.Id == 200);
        deletedMedia.IsDeleted = true;
        await context.SaveChangesAsync();
        var controller = new RentalHomesController(context);

        var list = GetObjectData<IReadOnlyList<RentalHomeResponse>>(await controller.GetList(default));
        var item = Assert.Single(list, home => home.Id == 101);
        Assert.Equal("/uploads/rental-homes/101/side.webp", item.MainImageUrl);

        var detail = GetObjectData<RentalHomeDetailResponse>(await controller.GetById(101, default));
        var media = Assert.Single(detail.MediaFiles);
        Assert.Equal("/uploads/rental-homes/101/side.webp", media.FileUrl);
    }

    [Fact]
    public async Task BrokerDetailExcludesDeletedMedia()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var deletedMedia = await context.MediaFiles.SingleAsync(item => item.Id == 200);
        deletedMedia.IsDeleted = true;
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var detail = GetData<BrokerRentalHomeDetailResponse>(await controller.GetById(101, default));

        var media = Assert.Single(detail.Media);
        Assert.Equal(201, media.Id);
        Assert.Equal("/uploads/rental-homes/101/side.webp", media.Url);
    }

    [Fact]
    public async Task DeletingMainMediaPromotesNextActiveImage()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.DeleteMedia(101, 200, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.True((await context.MediaFiles.IgnoreQueryFilters().SingleAsync(item => item.Id == 200)).IsDeleted);
        Assert.Equal(0, (await context.MediaFiles.SingleAsync(item => item.Id == 201)).SortOrder);
    }

    [Fact]
    public async Task BrokerCannotManageSoftDeletedRentalHome()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var home = await context.RentalHomes.SingleAsync(item => item.Id == 101);
        home.IsDeleted = true;
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        Assert.IsType<NotFoundObjectResult>(await controller.GetById(101, default));
        Assert.IsType<NotFoundObjectResult>(await controller.Update(101, ValidRequest("Should not update"), default));
        Assert.IsType<NotFoundObjectResult>(await controller.Publish(101, default));
        Assert.IsType<NotFoundObjectResult>(await controller.UploadMedia(101, ImageFile("home.webp", "image/webp"), default));
        Assert.IsType<NotFoundObjectResult>(await controller.AddAvailabilityBlock(101, new BrokerAvailabilityBlockRequest(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), null), default));
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
        context.Users.AddRange(
            new User { Id = 10, FullName = "Broker One", PhoneNumber = "+994501000010", Role = UserRole.Broker },
            new User { Id = 20, FullName = "Broker Two", PhoneNumber = "+994501000020", Role = UserRole.Broker });
        context.RentalHomes.AddRange(
            Home(101, 10, "Broker One Home"),
            Home(102, 20, "Broker Two Home"));
        context.MediaFiles.AddRange(
            Media(200, 101, "/uploads/rental-homes/101/main.webp", 0),
            Media(201, 101, "/uploads/rental-homes/101/side.webp", 2),
            Media(202, 102, "/uploads/rental-homes/102/main.webp", 0));
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
        Address = "Test address",
        DailyPrice = 120m,
        RoomCount = 3,
        GuestCount = 6,
        IsPublished = true
    };

    private static MediaFile Media(long id, long homeId, string url, int sortOrder) => new()
    {
        Id = id,
        RentalHomeId = homeId,
        FileType = MediaFileType.HomeImage,
        FileName = Path.GetFileName(url),
        FileUrl = url,
        ContentType = "image/webp",
        SizeBytes = 100,
        SortOrder = sortOrder
    };

    private static RentalHomeAvailabilityBlock Block(long id, long homeId, DateOnly startDate, DateOnly endDate, string? note) => new()
    {
        Id = id,
        RentalHomeId = homeId,
        StartDate = startDate,
        EndDate = endDate,
        Note = note
    };

    private static BrokerRentalHomeSaveRequest ValidRequest(string title) => new(
        title,
        "A clean broker property description.",
        "Qəbələ",
        "Vəndam",
        "Test address",
        140m,
        3,
        6,
        null);

    private static BrokerRentalHomesController CreateController(AppDbContext context, long brokerId)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "daily-rental-homes-tests", Guid.NewGuid().ToString("N"));
        return new BrokerRentalHomesController(context, new TestWebHostEnvironment(tempRoot))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreatePrincipal(brokerId, UserRole.Broker) }
            }
        };
    }

    private static IFormFile ImageFile(string fileName, string contentType)
    {
        var stream = new MemoryStream([1, 2, 3, 4]);
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
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

    private static T GetObjectData<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.True(response.Success);
        return Assert.IsAssignableFrom<T>(response.Data);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            WebRootPath = Path.Combine(contentRootPath, "wwwroot");
            Directory.CreateDirectory(WebRootPath);
        }

        public string ApplicationName { get; set; } = "DailyRentalHomes.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeFormFile : IFormFile
    {
        public FakeFormFile(string fileName, string contentType, long length)
        {
            FileName = fileName;
            ContentType = contentType;
            Length = length;
        }

        public string ContentType { get; }
        public string ContentDisposition { get; } = string.Empty;
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public long Length { get; }
        public string Name { get; } = "file";
        public string FileName { get; }
        public void CopyTo(Stream target) { }
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Stream OpenReadStream() => Stream.Null;
    }
}
