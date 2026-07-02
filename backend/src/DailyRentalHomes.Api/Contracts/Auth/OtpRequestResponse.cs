namespace DailyRentalHomes.Api.Contracts.Auth;

public sealed record OtpRequestResponse(
    string Message,
    DateTime ExpiresAt,
    string? DevPin = null);
