namespace DailyRentalHomes.Domain.Constants;

public static class NotificationTypeCodes
{
    public const string BookingCreated = "booking_created";
    public const string BookingCancellationRequested = "booking_cancellation_requested";
    public const string BookingStatusChanged = "booking_status_changed";
    public const string DepositRequested = "deposit_requested";
    public const string DepositDeadlineReminder = "deposit_deadline_reminder";
    public const string DepositReceiptUploaded = "deposit_receipt_uploaded";
    public const string DepositApproved = "deposit_approved";
    public const string DepositRejected = "deposit_rejected";
    public const string OtpCode = "otp_code";
    public const string ManualMessage = "manual_message";
    public const string LegacyDepositReminder = "legacy_deposit_reminder";
}
