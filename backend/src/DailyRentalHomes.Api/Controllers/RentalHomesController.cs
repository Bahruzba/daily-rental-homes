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
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

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
}
