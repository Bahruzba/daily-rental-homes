using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Broker;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Storage;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
[Route("api/broker/rental-homes")]
public sealed class BrokerRentalHomesController : ControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedImageTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
    };

    private readonly AppDbContext _db;
    private readonly IFileStorage _fileStorage;

    public BrokerRentalHomesController(AppDbContext db, IFileStorage fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    [HttpPost]
    public async Task<IActionResult> Create(BrokerRentalHomeSaveRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation));

        var home = new RentalHome
        {
            BrokerUserId = User.GetUserId(),
            Title = TextRules.Clean(request.Title),
            Description = TextRules.Clean(request.Description),
            City = TextRules.Clean(request.City),
            District = TextRules.CleanOptional(request.District),
            Address = TextRules.CleanOptional(request.Address),
            DailyPrice = request.DailyPrice,
            RoomCount = request.RoomCount,
            GuestCount = request.GuestCount,
            IsPublished = request.IsPublished ?? false,
            CreatedByUserId = User.GetUserId()
        };

        _db.RentalHomes.Add(home);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<BrokerRentalHomeSaveResponse>.Ok(new BrokerRentalHomeSaveResponse(home.Id)));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var home = await OwnHomes()
            .AsNoTracking()
            .Include(item => item.MediaFiles)
            .Include(item => item.AvailabilityBlocks)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (home is null) return NotFound(ApiResponse<object>.Fail("Rental home not found."));

        return Ok(ApiResponse<BrokerRentalHomeDetailResponse>.Ok(await ToDetail(home, cancellationToken)));
    }

    [HttpPost("{id:long}/duplicate")]
    public async Task<IActionResult> Duplicate(long id, CancellationToken cancellationToken)
    {
        var source = await OwnHomes()
            .AsNoTracking()
            .Include(item => item.Amenities)
            .Include(item => item.MediaFiles)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (source is null) return NotFound(ApiResponse<object>.Fail("Rental home not found."));

        var userId = User.GetUserId();
        var duplicate = new RentalHome
        {
            BrokerUserId = source.BrokerUserId,
            Title = source.Title,
            Description = source.Description,
            City = source.City,
            District = source.District,
            Address = source.Address,
            DailyPrice = source.DailyPrice,
            RoomCount = source.RoomCount,
            GuestCount = source.GuestCount,
            IsPublished = false,
            CreatedByUserId = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        foreach (var amenity in source.Amenities.Where(item => !item.IsDeleted))
        {
            duplicate.Amenities.Add(new RentalHomeAmenity
            {
                AmenityId = amenity.AmenityId,
                CreatedByUserId = userId
            });
        }

        foreach (var media in source.MediaFiles
                     .Where(item => !item.IsDeleted && item.FileType == MediaFileType.HomeImage)
                     .OrderBy(item => item.SortOrder)
                     .ThenBy(item => item.Id))
        {
            duplicate.MediaFiles.Add(new MediaFile
            {
                FileType = media.FileType,
                FileName = media.FileName,
                FileUrl = media.FileUrl,
                ContentType = media.ContentType,
                SizeBytes = media.SizeBytes,
                SortOrder = media.SortOrder,
                CreatedByUserId = userId
            });
        }

        _db.RentalHomes.Add(duplicate);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<BrokerRentalHomeSaveResponse>.Ok(new BrokerRentalHomeSaveResponse(duplicate.Id)));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, BrokerRentalHomeSaveRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation));

        var home = await OwnHomes().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (home is null) return NotFound(ApiResponse<object>.Fail("Rental home not found."));

        home.Title = TextRules.Clean(request.Title);
        home.Description = TextRules.Clean(request.Description);
        home.City = TextRules.Clean(request.City);
        home.District = TextRules.CleanOptional(request.District);
        home.Address = TextRules.CleanOptional(request.Address);
        home.DailyPrice = request.DailyPrice;
        home.RoomCount = request.RoomCount;
        home.GuestCount = request.GuestCount;
        if (request.IsPublished.HasValue) home.IsPublished = request.IsPublished.Value;
        home.UpdatedByUserId = User.GetUserId();

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<BrokerRentalHomeSaveResponse>.Ok(new BrokerRentalHomeSaveResponse(home.Id)));
    }

    [HttpPatch("{id:long}/publish")]
    public Task<IActionResult> Publish(long id, CancellationToken cancellationToken) =>
        SetPublication(id, true, cancellationToken);

    [HttpPatch("{id:long}/unpublish")]
    public Task<IActionResult> Unpublish(long id, CancellationToken cancellationToken) =>
        SetPublication(id, false, cancellationToken);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var home = await OwnHomes().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (home is null) return NotFound(ApiResponse<object>.Fail("Rental home not found."));

        home.IsDeleted = true;
        home.UpdatedByUserId = User.GetUserId();
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<BrokerRentalHomeSaveResponse>.Ok(new BrokerRentalHomeSaveResponse(home.Id)));
    }

    [HttpPost("{id:long}/media")]
    [RequestSizeLimit(MaxImageBytes + 64 * 1024)]
    public async Task<IActionResult> UploadMedia(long id, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0 || file.Length > MaxImageBytes ||
            !AllowedImageTypes.TryGetValue(file.ContentType, out var extension))
        {
            return BadRequest(ApiResponse<object>.Fail("Image must be a JPG, PNG or WebP file up to 5 MB."));
        }

        var home = await OwnHomes()
            .Include(item => item.MediaFiles)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (home is null) return NotFound(ApiResponse<object>.Fail("Rental home not found."));

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var storageKey = $"rental-homes/{id}/{storedName}";
        await using var input = file.OpenReadStream();
        var storedFile = await _fileStorage.SaveAsync(storageKey, input, cancellationToken);

        var activeMedia = home.MediaFiles
            .Where(item => !item.IsDeleted && item.FileType == MediaFileType.HomeImage)
            .OrderBy(item => item.SortOrder)
            .ToList();
        var isFirstImage = activeMedia.Count == 0;
        var media = new MediaFile
        {
            RentalHomeId = home.Id,
            FileType = MediaFileType.HomeImage,
            FileName = SafeFileName(file.FileName, extension),
            FileUrl = storedFile.Url,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            SortOrder = isFirstImage ? 0 : activeMedia.Max(item => item.SortOrder) + 1,
            CreatedByUserId = User.GetUserId()
        };

        home.MediaFiles.Add(media);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _fileStorage.DeleteAsync(storedFile.Key, CancellationToken.None);
            throw;
        }

        return Ok(ApiResponse<BrokerRentalHomeMediaUploadResponse>.Ok(ToUploadResponse(media)));
    }

    [HttpDelete("{id:long}/media/{mediaId:long}")]
    public async Task<IActionResult> DeleteMedia(long id, long mediaId, CancellationToken cancellationToken)
    {
        var media = await OwnMedia(id).FirstOrDefaultAsync(item => item.Id == mediaId, cancellationToken);
        if (media is null) return NotFound(ApiResponse<object>.Fail("Media file not found."));

        var wasMain = media.SortOrder == 0;
        media.IsDeleted = true;
        media.UpdatedByUserId = User.GetUserId();
        await _fileStorage.DeleteAsync(media.FileUrl, cancellationToken);
        if (wasMain)
        {
            var activeSiblings = await OwnMedia(id)
                .Where(item => item.Id != media.Id)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Id)
                .ToListAsync(cancellationToken);
            for (var index = 0; index < activeSiblings.Count; index++)
            {
                activeSiblings[index].SortOrder = index;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { media.Id }));
    }

    [HttpGet("{id:long}/availability-blocks")]
    public async Task<IActionResult> GetAvailabilityBlocks(long id, CancellationToken cancellationToken)
    {
        var homeExists = await OwnHomes().AnyAsync(item => item.Id == id, cancellationToken);
        if (!homeExists) return NotFound(ApiResponse<object>.Fail("Rental home not found."));

        var blocks = await _db.RentalHomeAvailabilityBlocks
            .AsNoTracking()
            .Where(item => item.RentalHomeId == id && !item.IsDeleted)
            .OrderBy(item => item.StartDate)
            .ThenBy(item => item.EndDate)
            .Select(item => new BrokerAvailabilityBlockResponse(
                item.Id,
                item.StartDate,
                item.EndDate,
                item.Note,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<BrokerAvailabilityBlockResponse>>.Ok(blocks));
    }

    [HttpPost("{id:long}/availability-blocks")]
    public async Task<IActionResult> AddAvailabilityBlock(
        long id,
        BrokerAvailabilityBlockRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateAvailabilityBlock(request);
        if (validation is not null) return BadRequest(ApiResponse<object>.Fail(validation));

        var homeExists = await OwnHomes().AnyAsync(item => item.Id == id, cancellationToken);
        if (!homeExists) return NotFound(ApiResponse<object>.Fail("Rental home not found."));

        var block = new RentalHomeAvailabilityBlock
        {
            RentalHomeId = id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Note = TextRules.CleanOptional(request.Note),
            CreatedByUserId = User.GetUserId()
        };
        _db.RentalHomeAvailabilityBlocks.Add(block);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<BrokerAvailabilityBlockResponse>.Ok(new BrokerAvailabilityBlockResponse(
            block.Id,
            block.StartDate,
            block.EndDate,
            block.Note,
            block.CreatedAt)));
    }

    [HttpDelete("{id:long}/availability-blocks/{blockId:long}")]
    public async Task<IActionResult> DeleteAvailabilityBlock(long id, long blockId, CancellationToken cancellationToken)
    {
        var block = await OwnHomes()
            .Where(home => home.Id == id)
            .SelectMany(home => home.AvailabilityBlocks)
            .FirstOrDefaultAsync(item => item.Id == blockId && !item.IsDeleted, cancellationToken);
        if (block is null) return NotFound(ApiResponse<object>.Fail("Availability block not found."));

        block.IsDeleted = true;
        block.UpdatedByUserId = User.GetUserId();
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { block.Id }));
    }

    [HttpPatch("{id:long}/media/{mediaId:long}/main")]
    public async Task<IActionResult> SetMainMedia(long id, long mediaId, CancellationToken cancellationToken)
    {
        var media = await OwnMedia(id).FirstOrDefaultAsync(item => item.Id == mediaId, cancellationToken);
        if (media is null) return NotFound(ApiResponse<object>.Fail("Media file not found."));

        var siblings = await OwnMedia(id).OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        var order = 1;
        foreach (var sibling in siblings.Where(item => item.Id != media.Id))
        {
            sibling.SortOrder = order++;
        }

        media.SortOrder = 0;
        media.UpdatedByUserId = User.GetUserId();
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<BrokerRentalHomeMediaUploadResponse>.Ok(ToUploadResponse(media)));
    }

    private async Task<IActionResult> SetPublication(long id, bool isPublished, CancellationToken cancellationToken)
    {
        var home = await OwnHomes().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (home is null) return NotFound(ApiResponse<object>.Fail("Rental home not found."));

        home.IsPublished = isPublished;
        home.UpdatedByUserId = User.GetUserId();
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<BrokerRentalHomeSaveResponse>.Ok(new BrokerRentalHomeSaveResponse(home.Id)));
    }

    private IQueryable<RentalHome> OwnHomes()
    {
        var query = _db.RentalHomes.Where(item => !item.IsDeleted);
        if (User.IsAdmin()) return query;
        var userId = User.GetUserId();
        return query.Where(item => item.BrokerUserId == userId);
    }

    private IQueryable<MediaFile> OwnMedia(long homeId) =>
        OwnHomes()
            .Where(home => home.Id == homeId)
            .SelectMany(home => home.MediaFiles)
            .Where(media => !media.IsDeleted && media.FileType == MediaFileType.HomeImage);

    private async Task<BrokerRentalHomeDetailResponse> ToDetail(RentalHome home, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var bookingCount = await _db.Bookings.CountAsync(item => item.RentalHomeId == home.Id, cancellationToken);
        var upcomingBookingCount = await _db.Bookings.CountAsync(
            item => item.RentalHomeId == home.Id && item.Dates.Any(date => date.Date >= today),
            cancellationToken);

        return new BrokerRentalHomeDetailResponse(
            home.Id,
            home.Title,
            home.Description,
            home.City,
            home.District,
            home.Address,
            home.DailyPrice,
            home.RoomCount,
            home.GuestCount,
            home.IsPublished,
            home.MediaFiles
                .Where(item => !item.IsDeleted && item.FileType == MediaFileType.HomeImage)
                .OrderBy(item => item.SortOrder)
                .Select(ToMediaResponse)
                .ToList(),
            home.AvailabilityBlocks
                .Where(item => !item.IsDeleted)
                .OrderBy(item => item.StartDate)
                .ThenBy(item => item.EndDate)
                .Select(item => new BrokerAvailabilityBlockResponse(
                    item.Id,
                    item.StartDate,
                    item.EndDate,
                    item.Note,
                    item.CreatedAt))
                .ToList(),
            bookingCount,
            upcomingBookingCount,
            home.CreatedAt,
            home.UpdatedAt);
    }

    private static BrokerRentalHomeMediaResponse ToMediaResponse(MediaFile media) => new(
        media.Id,
        media.FileUrl,
        media.FileType.ToString(),
        media.SortOrder == 0,
        media.SortOrder,
        media.ContentType,
        media.SizeBytes);

    private static BrokerRentalHomeMediaUploadResponse ToUploadResponse(MediaFile media) => new(
        media.Id,
        media.FileUrl,
        media.FileType.ToString(),
        media.SortOrder == 0,
        media.SortOrder,
        media.ContentType,
        media.SizeBytes);

    private static string SafeFileName(string value, string extension)
    {
        var originalName = Path.GetFileName(value);
        if (string.IsNullOrWhiteSpace(originalName)) originalName = $"home-image{extension}";
        return originalName[..Math.Min(originalName.Length, 255)];
    }

    private static string? Validate(BrokerRentalHomeSaveRequest request)
    {
        if (TextRules.Empty(request.Title)) return "Title is required.";
        if (request.Title.Trim().Length > 200) return "Title must be 200 characters or less.";
        if (request.Description?.Length > 4000) return "Description must be 4000 characters or less.";
        if (TextRules.Empty(request.City)) return "City is required.";
        if (request.City.Trim().Length > 100) return "City must be 100 characters or less.";
        if (request.District?.Length > 100) return "District must be 100 characters or less.";
        if (request.Address?.Length > 500) return "Address must be 500 characters or less.";
        if (TextRules.Empty(request.Address)) return "Address is required.";
        if (request.DailyPrice <= 0 || request.DailyPrice > 10000) return "Daily price must be between 1 and 10000.";
        if (request.RoomCount <= 0 || request.RoomCount > 50) return "Room count must be between 1 and 50.";
        if (request.GuestCount <= 0 || request.GuestCount > 100) return "Guest count must be between 1 and 100.";
        return null;
    }

    private static string? ValidateAvailabilityBlock(BrokerAvailabilityBlockRequest request)
    {
        if (request.StartDate == default || request.EndDate == default) return "Start and end dates are required.";
        if (request.StartDate > request.EndDate) return "Start date must not be after end date.";
        if (request.Note?.Length > 500) return "Note must be 500 characters or less.";
        return null;
    }
}
