using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed record DepositResponse(
    long Id,
    decimal Amount,
    string StatusCode,
    DateTime? DeadlineAt,
    string? CardHolderName,
    string? CardPanMasked,
    string? BankName,
    string? Note,
    DateTime RequestedAt,
    DateTime? UploadedAt,
    DateTime? ReviewedAt,
    string? ReviewNote,
    bool AllowReupload,
    DepositReceiptResponse? Receipt)
{
    public static DepositResponse FromEntity(BookingDeposit deposit)
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
            deposit.PaymentCard?.CardHolderName,
            deposit.PaymentCard?.PanMasked,
            deposit.PaymentCard?.BankName,
            deposit.Note,
            deposit.CreatedAt,
            deposit.UploadedAt,
            deposit.ReviewedAt,
            deposit.ReviewNote,
            deposit.AllowReupload,
            receipt);
    }
}

public sealed record DepositReceiptResponse(long Id, string FileName, string FileUrl, string? ContentType, long? SizeBytes);
