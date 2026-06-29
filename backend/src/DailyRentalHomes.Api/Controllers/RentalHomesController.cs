using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.RentalHomes;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
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
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.Id)
            .Select(x => new RentalHomeResponse(x.Id, x.Title, x.City, x.District, x.DailyPrice, x.RoomCount, x.GuestCount, x.IsPublished))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await _db.RentalHomes
            .AsNoTracking()
            .Include(x => x.MediaFiles)
            .Include(x => x.Contacts)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        return item is null ? NotFound(ApiResponse<object>.Fail("Rental home not found.")) : Ok(ApiResponse<object>.Ok(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRentalHomeRequest request, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(request.Title) || TextRules.Empty(request.City) || request.DailyPrice <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Title, city and positive daily price are required."));
        }

        var home = new RentalHome
        {
            BrokerUserId = request.BrokerUserId,
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

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var home = await _db.RentalHomes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (home is null)
        {
            return NotFound(ApiResponse<object>.Fail("Rental home not found."));
        }

        home.IsDeleted = true;
        home.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { home.Id }));
    }
}
