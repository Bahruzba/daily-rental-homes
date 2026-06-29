using DailyRentalHomes.Api.Contracts.Amenities;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/amenities")]
public sealed class AmenitiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AmenitiesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.Amenities.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AmenityInput request, CancellationToken cancellationToken)
    {
        var item = new Amenity
        {
            Name = request.Name,
            IconName = request.IconName
        };

        _db.Amenities.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(item.Id);
    }
}
