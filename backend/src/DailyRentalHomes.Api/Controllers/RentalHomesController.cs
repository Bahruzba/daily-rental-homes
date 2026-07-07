using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.RentalHomes;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/rental-homes")]
public sealed class RentalHomesController : ControllerBase
{
    private readonly AppDbContext _db;

    public RentalHomesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.RentalHomes
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsPublished)
            .OrderByDescending(x => x.Id)
            .Select(x => new RentalHomeResponse(
                x.Id,
                x.Title,
                x.City,
                x.District,
                x.DailyPrice,
                x.RoomCount,
                x.GuestCount,
                x.IsPublished,
                x.MediaFiles
                    .Where(media => !media.IsDeleted && media.FileType == DailyRentalHomes.Domain.Enums.MediaFileType.HomeImage)
                    .OrderBy(media => media.SortOrder)
                    .Select(media => media.FileUrl)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await _db.RentalHomes
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted && x.IsPublished)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.City,
                x.District,
                x.Address,
                x.DailyPrice,
                x.RoomCount,
                x.GuestCount,
                x.IsPublished,
                MediaFiles = x.MediaFiles
                    .Where(media => !media.IsDeleted && media.FileType == DailyRentalHomes.Domain.Enums.MediaFileType.HomeImage)
                    .OrderBy(media => media.SortOrder)
                    .Select(media => new RentalHomeMediaResponse(media.FileUrl, media.SortOrder))
                    .ToList(),
                Contacts = x.Contacts
                    .Select(contact => new RentalHomeContactResponse(contact.FullName, contact.Value, (int)contact.ContactType))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Rental home not found."));
        }

        var manualRanges = await _db.RentalHomeAvailabilityBlocks
            .AsNoTracking()
            .Where(block => !block.IsDeleted && block.RentalHomeId == id)
            .Select(block => new RentalHomeUnavailableRangeResponse(block.StartDate, block.EndDate))
            .ToListAsync(cancellationToken);
        var bookingRanges = await _db.Bookings
            .AsNoTracking()
            .Where(booking =>
                !booking.IsDeleted &&
                booking.RentalHomeId == id &&
                booking.Status != null &&
                booking.Status.Code != BookingStatusCodes.Cancelled &&
                booking.Status.Code != BookingStatusCodes.Rejected)
            .SelectMany(booking => booking.Dates.Where(date => !date.IsDeleted))
            .Select(date => new RentalHomeUnavailableRangeResponse(date.Date, date.Date))
            .ToListAsync(cancellationToken);
        var unavailableRanges = manualRanges
            .Concat(bookingRanges)
            .OrderBy(range => range.StartDate)
            .ThenBy(range => range.EndDate)
            .ToList();

        var response = new RentalHomeDetailResponse(
            item.Id,
            item.Title,
            item.Description,
            item.City,
            item.District,
            item.Address,
            item.DailyPrice,
            item.RoomCount,
            item.GuestCount,
            item.IsPublished,
            item.MediaFiles,
            item.Contacts,
            unavailableRanges);

        return Ok(ApiResponse<object>.Ok(response));
    }

    [Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateRentalHomeRequest request, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(request.Title) || TextRules.Empty(request.City) || request.DailyPrice <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Title, city and positive daily price are required."));
        }

        var brokerUserId = User.IsAdmin() ? request.BrokerUserId : User.GetUserId();
        if (brokerUserId <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Broker is required."));
        }

        var home = new RentalHome
        {
            BrokerUserId = brokerUserId,
            Title = TextRules.Clean(request.Title),
            Description = TextRules.Clean(request.Description),
            City = TextRules.Clean(request.City),
            District = TextRules.CleanOptional(request.District),
            Address = TextRules.CleanOptional(request.Address),
            DailyPrice = request.DailyPrice,
            RoomCount = request.RoomCount,
            GuestCount = request.GuestCount
        };

        _db.RentalHomes.Add(home);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { home.Id }));
    }

    [Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateRentalHomeRequest request, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(request.Title) || TextRules.Empty(request.City) || request.DailyPrice <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Title, city and positive daily price are required."));
        }

        var home = await _db.RentalHomes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (home is null)
        {
            return NotFound(ApiResponse<object>.Fail("Rental home not found."));
        }

        if (!User.IsAdmin() && home.BrokerUserId != User.GetUserId())
        {
            return Forbid();
        }

        home.Title = TextRules.Clean(request.Title);
        home.Description = TextRules.Clean(request.Description);
        home.City = TextRules.Clean(request.City);
        home.District = TextRules.CleanOptional(request.District);
        home.Address = TextRules.CleanOptional(request.Address);
        home.DailyPrice = request.DailyPrice;
        home.RoomCount = request.RoomCount;
        home.GuestCount = request.GuestCount;
        home.IsPublished = request.IsPublished;
        home.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { home.Id }));
    }

    [Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var home = await _db.RentalHomes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (home is null)
        {
            return NotFound(ApiResponse<object>.Fail("Rental home not found."));
        }

        if (!User.IsAdmin() && home.BrokerUserId != User.GetUserId())
        {
            return Forbid();
        }

        home.IsDeleted = true;
        home.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { home.Id }));
    }
}
