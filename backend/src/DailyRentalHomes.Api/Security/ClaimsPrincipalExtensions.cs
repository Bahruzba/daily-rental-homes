using DailyRentalHomes.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static long GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!long.TryParse(value, out var userId) || userId <= 0)
        {
            throw new InvalidOperationException("The authenticated user id claim is missing or invalid.");
        }

        return userId;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole(nameof(UserRole.Admin));
}
