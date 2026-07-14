using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Deposits;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
[Route("api/broker/bookings/{bookingId:long}/deposit")]
public sealed class BrokerDepositsController : ControllerBase
{
    private static readonly HashSet<string> DeadlineExtensionBlockedBookingStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        BookingStatusCodes.Cancelled,
        BookingStatusCodes.Completed,
        BookingStatusCodes.Rejected
    };

    private readonly AppDbContext _db;
    private readonly INotificationOutboxService _notifications;

    public BrokerDepositsController(AppDbContext db, INotificationOutboxService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestDeposit(
        long bookingId,
        RequestBookingDepositInput input,
        CancellationToken cancellationToken)
    {
        if (input.Amount <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Deposit amount must be greater than zero."));
        }

        if (input.DeadlineAt <= DateTime.UtcNow)
        {
            return BadRequest(ApiResponse<object>.Fail("Deposit deadline must be in the future."));
        }

        if (!IsSafeMaskedPan(input.CardPanMasked))
        {
            return BadRequest(ApiResponse<object>.Fail("A masked card number is required; full card PAN must not be stored."));
        }

        if ((!string.IsNullOrWhiteSpace(input.CardHolderName) && input.CardHolderName.Trim().Length > 150) ||
            (!string.IsNullOrWhiteSpace(input.BankName) && input.BankName.Trim().Length > 100) ||
            (!string.IsNullOrWhiteSpace(input.Note) && input.Note.Trim().Length > 1000))
        {
            return BadRequest(ApiResponse<object>.Fail("Deposit instruction fields exceed the allowed length."));
        }

        var booking = await ScopeBookings(_db.Bookings)
            .Include(item => item.RentalHome)
            .Include(item => item.Status)
            .Include(item => item.Deposit)
            .FirstOrDefaultAsync(item => item.Id == bookingId, cancellationToken);

        if (booking is null)
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        if (booking.Deposit is not null)
        {
            return BadRequest(ApiResponse<object>.Fail("An active deposit request already exists for this booking."));
        }

        var currentStatusCode = booking.Status?.Code ?? string.Empty;
        if (currentStatusCode is not BookingStatusCodes.Pending and not BookingStatusCodes.WaitingDeposit)
        {
            return BadRequest(ApiResponse<object>.Fail("A deposit cannot be requested for the current booking status."));
        }

        var waitingStatus = await FindBookingStatus(BookingStatusCodes.WaitingDeposit, cancellationToken);
        if (waitingStatus is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Fail("Waiting deposit booking status is not configured."));
        }

        var brokerUserId = User.GetUserId();
        var brokerName = await _db.Users.AsNoTracking()
            .Where(user => user.Id == brokerUserId)
            .Select(user => user.FullName)
            .FirstOrDefaultAsync(cancellationToken);
        var paymentCard = new PaymentCard
        {
            BrokerUserId = brokerUserId,
            CardHolderName = TextRules.CleanOptional(input.CardHolderName) ?? brokerName ?? "Broker",
            PanMasked = TextRules.Clean(input.CardPanMasked),
            BankName = TextRules.CleanOptional(input.BankName),
            CreatedByUserId = brokerUserId
        };
        var deposit = new BookingDeposit
        {
            BookingId = booking.Id,
            Amount = input.Amount,
            DeadlineAt = input.DeadlineAt,
            Status = BookingDepositStatus.Waiting,
            PaymentCard = paymentCard,
            Note = TextRules.CleanOptional(input.Note),
            CreatedByUserId = brokerUserId,
            AllowReupload = true
        };

        _db.BookingDeposits.Add(deposit);
        if (booking.StatusId != waitingStatus.Id)
        {
            AddBookingStatusHistory(booking, waitingStatus, "Deposit requested.", brokerUserId);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _notifications.QueueDepositRequestedAsync(booking, deposit, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<DepositResponse>.Ok(DepositResponse.FromEntity(deposit)));
    }

    [HttpPost("extend-deadline")]
    public async Task<IActionResult> ExtendDeadline(
        long bookingId,
        ExtendDepositDeadlineRequest input,
        CancellationToken cancellationToken)
    {
        var reason = TextRules.CleanOptional(input.Reason);
        if (reason?.Length > 500)
        {
            return BadRequest(ApiResponse<object>.Fail("Deposit deadline extension reason must be 500 characters or less."));
        }

        var now = DateTime.UtcNow;
        if (input.DeadlineAt <= now)
        {
            return BadRequest(ApiResponse<object>.Fail("Deposit deadline must be in the future."));
        }

        var booking = await GetBookingWithDeposit(bookingId, cancellationToken);
        if (booking is null)
        {
            return NotFound(ApiResponse<object>.Fail("Booking or deposit not found."));
        }

        var deposit = booking.Deposit!;
        if (!deposit.DeadlineAt.HasValue || input.DeadlineAt <= deposit.DeadlineAt.Value)
        {
            return BadRequest(ApiResponse<object>.Fail("New deposit deadline must be later than the current deadline."));
        }

        if (deposit.Status == BookingDepositStatus.Paid)
        {
            return BadRequest(ApiResponse<object>.Fail("Approved deposits cannot be extended."));
        }

        if (DeadlineExtensionBlockedBookingStatuses.Contains(booking.Status?.Code ?? string.Empty))
        {
            return BadRequest(ApiResponse<object>.Fail("Deposit deadline cannot be extended for the current booking status."));
        }

        var userId = User.GetUserId();
        deposit.DeadlineAt = input.DeadlineAt;
        deposit.DeadlineExtendedAt = now;
        deposit.DeadlineExtendedByUserId = userId;
        deposit.DeadlineExtensionReason = reason;
        deposit.UpdatedByUserId = userId;

        await _notifications.QueueDepositDeadlineExtendedAsync(booking, deposit, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ExtendDepositDeadlineResponse>.Ok(new ExtendDepositDeadlineResponse(
            booking.Id,
            deposit.Id,
            deposit.DeadlineAt.Value,
            deposit.DeadlineExtendedAt.Value,
            deposit.DeadlineExtensionReason)));
    }

    [HttpPost("approve")]
    public async Task<IActionResult> Approve(
        long bookingId,
        ReviewBookingDepositInput input,
        CancellationToken cancellationToken)
    {
        var booking = await GetBookingWithDeposit(bookingId, cancellationToken);
        if (booking is null)
        {
            return NotFound(ApiResponse<object>.Fail("Booking or deposit not found."));
        }

        if (booking.Deposit!.Status != BookingDepositStatus.ReceiptUploaded ||
            !booking.Deposit.ReceiptFiles.Any(file => file.FileType == MediaFileType.DepositReceipt && !file.IsDeleted))
        {
            return BadRequest(ApiResponse<object>.Fail("An uploaded deposit receipt is required before approval."));
        }

        if (booking.Status?.Code != BookingStatusCodes.WaitingDeposit)
        {
            return BadRequest(ApiResponse<object>.Fail("The booking is not waiting for a deposit review."));
        }

        var confirmedStatus = await FindBookingStatus(BookingStatusCodes.Confirmed, cancellationToken);
        if (confirmedStatus is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Fail("Confirmed booking status is not configured."));
        }

        var userId = User.GetUserId();
        booking.Deposit.Status = BookingDepositStatus.Paid;
        booking.Deposit.PaidAt = DateTime.UtcNow;
        booking.Deposit.ReviewedAt = DateTime.UtcNow;
        booking.Deposit.ReviewedByUserId = userId;
        booking.Deposit.ReviewNote = TextRules.CleanOptional(input.Note);
        booking.Deposit.AllowReupload = false;
        AddBookingStatusHistory(booking, confirmedStatus, input.Note ?? "Deposit approved.", userId);

        await _notifications.QueueDepositApprovedAsync(booking, booking.Deposit, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<DepositResponse>.Ok(DepositResponse.FromEntity(booking.Deposit)));
    }

    [HttpPost("reject")]
    public async Task<IActionResult> Reject(
        long bookingId,
        ReviewBookingDepositInput input,
        CancellationToken cancellationToken)
    {
        var booking = await GetBookingWithDeposit(bookingId, cancellationToken);
        if (booking is null)
        {
            return NotFound(ApiResponse<object>.Fail("Booking or deposit not found."));
        }

        if (booking.Deposit!.Status != BookingDepositStatus.ReceiptUploaded)
        {
            return BadRequest(ApiResponse<object>.Fail("Only an uploaded receipt can be rejected."));
        }

        if (booking.Status?.Code != BookingStatusCodes.WaitingDeposit)
        {
            return BadRequest(ApiResponse<object>.Fail("The booking is not waiting for a deposit review."));
        }

        booking.Deposit.Status = BookingDepositStatus.Rejected;
        booking.Deposit.ReviewedAt = DateTime.UtcNow;
        booking.Deposit.ReviewedByUserId = User.GetUserId();
        booking.Deposit.ReviewNote = TextRules.CleanOptional(input.Note);
        booking.Deposit.AllowReupload = input.AllowReupload;

        await _notifications.QueueDepositRejectedAsync(booking, booking.Deposit, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<DepositResponse>.Ok(DepositResponse.FromEntity(booking.Deposit)));
    }

    private async Task<Booking?> GetBookingWithDeposit(long bookingId, CancellationToken cancellationToken) =>
        await ScopeBookings(_db.Bookings)
            .Include(item => item.RentalHome)
            .Include(item => item.Status)
            .Include(item => item.Deposit)!.ThenInclude(deposit => deposit!.PaymentCard)
            .Include(item => item.Deposit)!.ThenInclude(deposit => deposit!.ReceiptFiles)
            .FirstOrDefaultAsync(item => item.Id == bookingId && item.Deposit != null, cancellationToken);

    private IQueryable<Booking> ScopeBookings(IQueryable<Booking> query)
    {
        if (User.IsAdmin()) return query;
        var userId = User.GetUserId();
        return query.Where(booking => booking.RentalHome!.BrokerUserId == userId);
    }

    private async Task<BookingStatus?> FindBookingStatus(string code, CancellationToken cancellationToken) =>
        await _db.BookingStatuses.FirstOrDefaultAsync(
            status => status.Code == code && status.IsActive,
            cancellationToken);

    private void AddBookingStatusHistory(Booking booking, BookingStatus newStatus, string? note, long userId)
    {
        var oldStatusId = booking.StatusId;
        booking.StatusId = newStatus.Id;
        booking.UpdatedByUserId = userId;
        _db.BookingStatusHistory.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            OldStatusId = oldStatusId,
            NewStatusId = newStatus.Id,
            ChangedByUserId = userId,
            Note = TextRules.CleanOptional(note)
        });
    }

    private static bool IsSafeMaskedPan(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 30 || value.Count(character => character == '*') < 4) return false;
        return value.Count(char.IsDigit) == 4 && value.All(character => char.IsDigit(character) || character is ' ' or '-' or '*');
    }
}
