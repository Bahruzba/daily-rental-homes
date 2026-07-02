using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.MediaFiles;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/media-files")]
public sealed class MediaFilesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MediaFilesController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.MediaFiles.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(items));
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost]
    public async Task<IActionResult> Create(NewMediaFileRequest request, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(request.FileName) || TextRules.Empty(request.FileUrl))
        {
            return BadRequest(ApiResponse<object>.Fail("File name and url are required."));
        }

        var file = new MediaFile
        {
            RentalHomeId = request.RentalHomeId,
            BookingDepositId = request.BookingDepositId,
            FileType = request.FileType,
            FileName = TextRules.Clean(request.FileName),
            FileUrl = TextRules.Clean(request.FileUrl),
            ContentType = TextRules.CleanOptional(request.ContentType),
            SizeBytes = request.SizeBytes,
            SortOrder = request.SortOrder
        };

        _db.MediaFiles.Add(file);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { file.Id }));
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var file = await _db.MediaFiles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (file is null)
        {
            return NotFound(ApiResponse<object>.Fail("Media file not found."));
        }

        file.IsDeleted = true;
        file.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { file.Id }));
    }
}
