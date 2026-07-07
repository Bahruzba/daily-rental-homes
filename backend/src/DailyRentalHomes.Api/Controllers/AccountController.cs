using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Account;
using DailyRentalHomes.Api.Contracts.Deposits;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.CustomerOnly)]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private const long MaxReceiptBytes = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedImageTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly INotificationOutboxService _notifications;

    public AccountController(AppDbContext db, IWebHostEnvironment environment, INotificationOutboxService notifications)
    {
        _db = db;
        _environment = environment;
        _notifications = notifications;
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings(CancellationToken cancellationToken)
    {
        var bookings = await ScopeBookings(_db.Bookings.AsNoTracking())
            .Include(item => item.RentalHome)
            .Include(item => item.Status)
            .Include(item => item.Dates)
            .Include(item => item.Deposit)!.ThenInclude(deposit => deposit!.PaymentCard)
            .Include(item => item.Deposit)!.ThenInclude(deposit => deposit!.ReceiptFiles)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);

        var response = bookings.Select(item => new AccountBookingListItemResponse(
            item.Id,
            item.RentalHome?.Title ?? string.Empty,
            item.RentalHome?.City ?? string.Empty,
            item.RentalHome?.District,
            item.Status?.Code ?? string.Empty,
            item.Status?.Name ?? string.Empty,
            item.TotalAmount,
            item.Dates.OrderBy(date => date.Date).Select(date => date.Date).ToList(),
            item.Deposit is null ? null : DepositResponse.FromEntity(item.Deposit),
            item.CreatedAt)).ToList();

        return Ok(ApiResponse<IReadOnlyList<AccountBookingListItemResponse>>.Ok(response));
    }

    [HttpGet("bookings/{id:long}")]
    public async Task<IActionResult> GetBookingById(long id, CancellationToken cancellationToken)
    {
        var booking = await BookingDetails()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (booking is null)
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        return Ok(ApiResponse<AccountBookingDetailResponse>.Ok(ToDetail(booking)));
    }

    [HttpPost("bookings/{id:long}/deposit/receipt")]
    [RequestSizeLimit(MaxReceiptBytes + 64 * 1024)]
    public async Task<IActionResult> UploadDepositReceipt(
        long id,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0 || file.Length > MaxReceiptBytes ||
            !AllowedImageTypes.TryGetValue(file.ContentType, out var extension))
        {
            return BadRequest(ApiResponse<object>.Fail("Receipt must be a JPG, PNG or WebP image up to 5 MB."));
        }

        var booking = await BookingDetails()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (booking is null)
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        var deposit = booking.Deposit;
        if (deposit is null)
        {
            return BadRequest(ApiResponse<object>.Fail("A deposit has not been requested for this booking."));
        }

        var canUpload = deposit.Status == BookingDepositStatus.Waiting ||
                        (deposit.Status == BookingDepositStatus.Rejected && deposit.AllowReupload);
        if (!canUpload)
        {
            return BadRequest(ApiResponse<object>.Fail("A receipt cannot be uploaded for the current deposit status."));
        }

        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        var uploadDirectory = Path.Combine(webRoot, "uploads", "deposit-receipts");
        Directory.CreateDirectory(uploadDirectory);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadDirectory, storedName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        foreach (var existing in deposit.ReceiptFiles.Where(item => item.FileType == MediaFileType.DepositReceipt && !item.IsDeleted))
        {
            existing.IsDeleted = true;
        }

        var originalName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalName)) originalName = $"receipt{extension}";
        var receipt = new MediaFile
        {
            BookingDepositId = deposit.Id,
            FileType = MediaFileType.DepositReceipt,
            FileName = originalName[..Math.Min(originalName.Length, 255)],
            FileUrl = $"/uploads/deposit-receipts/{storedName}",
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            CreatedByUserId = User.GetUserId()
        };
        deposit.ReceiptFiles.Add(receipt);
        deposit.Status = BookingDepositStatus.ReceiptUploaded;
        deposit.UploadedAt = DateTime.UtcNow;
        deposit.ReviewedAt = null;
        deposit.ReviewedByUserId = null;
        deposit.ReviewNote = null;

        try
        {
            await _notifications.QueueDepositReceiptUploadedAsync(booking, deposit, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            System.IO.File.Delete(fullPath);
            throw;
        }

        return Ok(ApiResponse<DepositResponse>.Ok(DepositResponse.FromEntity(deposit)));
    }

    private IQueryable<Booking> BookingDetails() => ScopeBookings(_db.Bookings)
        .AsSplitQuery()
        .Include(item => item.RentalHome)
        .Include(item => item.Status)
        .Include(item => item.Dates)
        .Include(item => item.Deposit)!.ThenInclude(deposit => deposit!.PaymentCard)
        .Include(item => item.Deposit)!.ThenInclude(deposit => deposit!.ReceiptFiles);

    private IQueryable<Booking> ScopeBookings(IQueryable<Booking> query)
    {
        var userId = User.GetUserId();
        var phone = _db.Users.Where(user => user.Id == userId).Select(user => user.PhoneNumber).FirstOrDefault();
        return query.Where(booking => booking.CustomerUserId == userId || booking.CustomerPhoneNumber == phone);
    }

    private static AccountBookingDetailResponse ToDetail(Booking booking) => new(
        booking.Id,
        booking.RentalHomeId,
        booking.RentalHome?.Title ?? string.Empty,
        booking.RentalHome?.City ?? string.Empty,
        booking.RentalHome?.District,
        booking.Status?.Code ?? string.Empty,
        booking.Status?.Name ?? string.Empty,
        booking.DailyPrice,
        booking.TotalAmount,
        booking.GuestCount,
        booking.Dates.OrderBy(date => date.Date).Select(date => date.Date).ToList(),
        booking.CustomerNote,
        booking.Deposit is null ? null : DepositResponse.FromEntity(booking.Deposit),
        booking.CreatedAt);
}
