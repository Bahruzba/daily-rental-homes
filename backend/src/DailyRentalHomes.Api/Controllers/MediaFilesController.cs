using DailyRentalHomes.Api.Contracts.MediaFiles;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
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

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.MediaFiles.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewMediaFileRequest request, CancellationToken cancellationToken)
    {
        var file = new MediaFile
        {
            RentalHomeId = request.RentalHomeId,
            BookingDepositId = request.BookingDepositId,
            FileType = request.FileType,
            FileName = request.FileName,
            FileUrl = request.FileUrl,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            SortOrder = request.SortOrder
        };

        _db.MediaFiles.Add(file);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(file.Id);
    }
}
