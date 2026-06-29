using DailyRentalHomes.Api.Common;
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
        var items = await _db.Amenities.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPost]
    public async Task<IActionResult> Create(AmenityInput request, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("Name is required."));
        }

        var item = new Amenity
        {
            Name = TextRules.Clean(request.Name),
            IconName = TextRules.CleanOptional(request.IconName)
        };

        _db.Amenities.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { item.Id }));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, AmenityInput request, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(request.Name))
        {
            return BadRequest(ApiResponse<object>.Fail("Name is required."));
        }

        var item = await _db.Amenities.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Amenity not found."));
        }

        item.Name = TextRules.Clean(request.Name);
        item.IconName = TextRules.CleanOptional(request.IconName);
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { item.Id }));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var item = await _db.Amenities.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Amenity not found."));
        }

        item.IsDeleted = true;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { item.Id }));
    }
}
