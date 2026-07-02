namespace DailyRentalHomes.Api.Contracts.Auth;

public sealed record AuthSessionResponse(
    string AccessToken,
    DateTime ExpiresAt,
    AuthUserResponse User);

public sealed record AuthUserResponse(
    long Id,
    string FullName,
    string Phone,
    string Role);
