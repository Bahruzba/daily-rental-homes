using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed record DepositResponse(
    long Id,
    decimal Amount,
    string StatusCode,
    DateTime? DeadlineAt,
    DateTime? DeadlineExtendedAt,
    string? DeadlineExtensionReason,
    string? CardHolderName,
    string? CardPanMasked,
    string? BankName,
    string? Note,
    DateTime RequestedAt,
    DateTime? UploadedAt,
    DateTime? ReviewedAt,
    string? ReviewNote,
    bool AllowReupload,
    bool IsDeadlineExpired,
    DepositReceiptResponse? Receipt)
{
    private static readonly string[] InactiveBookingStatuses =
    [
        BookingStatusCodes.Cancelled,
        BookingStatusCodes.Completed,
        BookingStatusCodes.Rejected
    ];

    public static DepositResponse FromEntity(BookingDeposit deposit, string? bookingStatusCode = null)
    {
        var receipt = deposit.ReceiptFiles
            .Where(file => file.FileType == MediaFileType.DepositReceipt && !file.IsDeleted)
            .OrderByDescending(file => file.Id)
            .Select(file => new DepositReceiptResponse(file.Id, file.FileName, file.FileUrl, file.ContentType, file.SizeBytes))
            .FirstOrDefault();

        return new DepositResponse(
            deposit.Id,
            deposit.Amount,
            DepositStatusCodes.FromStatus(deposit.Status),
            deposit.DeadlineAt,
            deposit.DeadlineExtendedAt,
            deposit.DeadlineExtensionReason,
            deposit.PaymentCard?.CardHolderName,
            deposit.PaymentCard?.PanMasked,
            deposit.PaymentCard?.BankName,
            deposit.Note,
            deposit.CreatedAt,
            deposit.UploadedAt,
            deposit.ReviewedAt,
            deposit.ReviewNote,
            deposit.AllowReupload,
            IsDeadlineExpiredFor(deposit, bookingStatusCode),
            receipt);
    }

    public static bool IsDeadlineExpiredFor(BookingDeposit deposit, string? bookingStatusCode = null, DateTime? now = null)
    {
        var statusCode = bookingStatusCode ?? deposit.Booking?.Status?.Code;
        return deposit.DeadlineAt.HasValue &&
               deposit.DeadlineAt.Value < (now ?? DateTime.UtcNow) &&
               deposit.Status != BookingDepositStatus.Paid &&
               !InactiveBookingStatuses.Contains(statusCode ?? string.Empty);
    }
}

public sealed record DepositReceiptResponse(long Id, string FileName, string FileUrl, string? ContentType, long? SizeBytes);
