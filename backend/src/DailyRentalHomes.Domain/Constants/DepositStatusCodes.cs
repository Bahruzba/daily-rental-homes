using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Domain.Constants;

public static class DepositStatusCodes
{
    public const string Requested = "requested";
    public const string ReceiptUploaded = "receipt_uploaded";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Expired = "expired";
    public const string Cancelled = "cancelled";

    public static string FromStatus(BookingDepositStatus status) => status switch
    {
        BookingDepositStatus.Waiting => Requested,
        BookingDepositStatus.ReceiptUploaded => ReceiptUploaded,
        BookingDepositStatus.Paid => Approved,
        BookingDepositStatus.Rejected => Rejected,
        BookingDepositStatus.Expired => Expired,
        BookingDepositStatus.Cancelled => Cancelled,
        _ => status.ToString().ToLowerInvariant()
    };
}
