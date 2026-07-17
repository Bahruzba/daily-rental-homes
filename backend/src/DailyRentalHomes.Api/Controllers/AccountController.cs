using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Account;
using DailyRentalHomes.Api.Contracts.Deposits;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Api.Storage;
using DailyRentalHomes.Domain.Constants;
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
    private const string CancellationRequestPending = "pending";
    private static readonly HashSet<string> CancellableStatusCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        BookingStatusCodes.Pending,
        BookingStatusCodes.WaitingDeposit,
        BookingStatusCodes.Confirmed,
        BookingStatusCodes.Paid
    };
    private static readonly IReadOnlyDictionary<string, string> AllowedImageTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    private readonly AppDbContext _db;
    private readonly IFileStorage _fileStorage;
    private readonly INotificationOutboxService _notifications;

    public AccountController(AppDbContext db, IFileStorage fileStorage, INotificationOutboxService notifications)
    {
        _db = db;
        _fileStorage = fileStorage;
        _notifications = notifications;
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings(CancellationToken cancellationToken)
    {
        var bookings = await ScopeBookings(_db.Bookings.AsNoTracking())
            .Include(item => item.RentalHome)
            .ThenInclude(home => home!.MediaFiles)
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
            MainImageUrl(item),
            item.Status?.Code ?? string.Empty,
            item.Status?.Name ?? string.Empty,
            item.TotalAmount,
            item.Dates.OrderBy(date => date.Date).Select(date => date.Date).ToList(),
            item.Deposit is null ? null : DepositResponse.FromEntity(item.Deposit, item.Status?.Code),
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

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var storageKey = $"deposit-receipts/{storedName}";
        await using var input = file.OpenReadStream();
        var storedFile = await _fileStorage.SavePrivateAsync(storageKey, input, cancellationToken);

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
            FileUrl = storedFile.Url,
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
            await _fileStorage.DeleteAsync(storedFile.Key, CancellationToken.None);
            throw;
        }

        return Ok(ApiResponse<DepositResponse>.Ok(DepositResponse.FromEntity(deposit, booking.Status?.Code)));
    }

    [HttpPost("bookings/{id:long}/cancellation-requests")]
    public async Task<IActionResult> RequestCancellation(
        long id,
        CreateBookingCancellationRequest request,
        CancellationToken cancellationToken)
    {
        var reason = request.Reason?.Trim();
        if (reason?.Length > 1000)
        {
            return BadRequest(ApiResponse<object>.Fail("Cancellation reason must be 1000 characters or less."));
        }

        var booking = await BookingDetails()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (booking is null)
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        if (!CancellableStatusCodes.Contains(booking.Status?.Code ?? string.Empty))
        {
            return BadRequest(ApiResponse<object>.Fail("A cancellation request can be sent only for active bookings."));
        }

        var existingPending = booking.CancellationRequests
            .Where(item => item.StatusCode == CancellationRequestPending)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();
        if (existingPending is not null)
        {
            return BadRequest(ApiResponse<object>.Fail("A pending cancellation request already exists for this booking."));
        }

        var cancellationRequest = new BookingCancellationRequest
        {
            BookingId = booking.Id,
            RequestedByUserId = User.GetUserId(),
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason,
            StatusCode = CancellationRequestPending,
            CreatedByUserId = User.GetUserId()
        };
        booking.CancellationRequests.Add(cancellationRequest);

        await _notifications.QueueBookingCancellationRequestedAsync(booking, cancellationRequest, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<BookingCancellationRequestResponse>.Ok(ToCancellationResponse(cancellationRequest)));
    }

    private IQueryable<Booking> BookingDetails() => ScopeBookings(_db.Bookings)
        .AsSplitQuery()
        .Include(item => item.RentalHome)
        .ThenInclude(home => home!.MediaFiles)
        .Include(item => item.Status)
        .Include(item => item.Dates)
        .Include(item => item.CancellationRequests)
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
        MainImageUrl(booking),
        booking.Status?.Code ?? string.Empty,
        booking.Status?.Name ?? string.Empty,
        booking.DailyPrice,
        booking.TotalAmount,
        booking.GuestCount,
        booking.Dates.OrderBy(date => date.Date).Select(date => date.Date).ToList(),
        booking.CustomerNote,
        booking.Deposit is null ? null : DepositResponse.FromEntity(booking.Deposit, booking.Status?.Code),
        booking.CreatedAt,
        booking.CancellationRequests.Any(item => item.StatusCode == CancellationRequestPending));

    private static BookingCancellationRequestResponse ToCancellationResponse(BookingCancellationRequest request) => new(
        request.Id,
        request.BookingId,
        request.StatusCode,
        request.Reason,
        request.CreatedAt);

    private static string? MainImageUrl(Booking booking) => booking.RentalHome?.MediaFiles
        .Where(file => file.FileType == MediaFileType.HomeImage && !file.IsDeleted)
        .OrderBy(file => file.SortOrder)
        .Select(file => file.FileUrl)
        .FirstOrDefault();
}
