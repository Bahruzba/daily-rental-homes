using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Storage;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings/{bookingId:long}/deposit/receipt")]
public sealed class DepositReceiptsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _fileStorage;

    public DepositReceiptsController(AppDbContext db, IFileStorage fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    [HttpGet]
    public async Task<IActionResult> Download(long bookingId, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(ApiResponse<object>.Fail("Authentication is required."));
        }

        var booking = await _db.Bookings
            .AsNoTracking()
            .Include(item => item.RentalHome)
            .Include(item => item.Deposit)!.ThenInclude(deposit => deposit!.ReceiptFiles)
            .FirstOrDefaultAsync(item => item.Id == bookingId, cancellationToken);

        if (booking is null || !CanAccess(booking.CustomerUserId, booking.CustomerPhoneNumber, booking.RentalHome?.BrokerUserId))
        {
            return NotFound(ApiResponse<object>.Fail("Receipt not found."));
        }

        var receipt = booking.Deposit?.ReceiptFiles
            .Where(file => file.FileType == MediaFileType.DepositReceipt && !file.IsDeleted)
            .OrderByDescending(file => file.Id)
            .FirstOrDefault();
        if (receipt is null)
        {
            return NotFound(ApiResponse<object>.Fail("Receipt not found."));
        }

        var storedFile = await _fileStorage.OpenReadAsync(receipt.FileUrl, cancellationToken);
        if (storedFile is null)
        {
            return NotFound(ApiResponse<object>.Fail("Receipt not found."));
        }

        Response.Headers.CacheControl = "no-store, private";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        return File(storedFile.Content, receipt.ContentType ?? "application/octet-stream", SafeFileName(receipt.FileName));
    }

    private bool CanAccess(long? customerUserId, string customerPhoneNumber, long? brokerUserId)
    {
        if (User.IsAdmin()) return true;

        var userId = User.GetUserId();
        if (customerUserId == userId) return true;
        if (brokerUserId == userId) return true;

        var phone = _db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.PhoneNumber)
            .FirstOrDefault();
        return !string.IsNullOrWhiteSpace(phone) &&
               string.Equals(phone, customerPhoneNumber, StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeFileName(string value)
    {
        var fileName = Path.GetFileName(value);
        return string.IsNullOrWhiteSpace(fileName) ? "deposit-receipt" : fileName;
    }
}
