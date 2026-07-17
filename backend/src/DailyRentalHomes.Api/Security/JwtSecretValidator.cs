using Microsoft.Extensions.Hosting;

namespace DailyRentalHomes.Api.Security;

public static class JwtSecretValidator
{
    private static readonly string[] UnsafeMarkers =
    [
        "LOCAL_DEVELOPMENT_KEY",
        "CHANGE_ME",
        "DO_NOT_COMMIT",
        "TEST_SIGNING_KEY",
        "PLACEHOLDER",
        "YOUR_SECRET",
        "YOUR_",
        "DEVELOPMENT_KEY"
    ];

    public static bool IsAllowed(string? key, string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (IsDevelopment(environmentName)) return true;

        return !UnsafeMarkers.Any(marker => key.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    public static void ThrowIfUnsafe(string? key, string? environmentName)
    {
        if (IsAllowed(key, environmentName)) return;

        throw new InvalidOperationException(
            "Token key is missing or uses a known development/placeholder value. Configure Token:Key with a unique production secret.");
    }

    private static bool IsDevelopment(string? environmentName) =>
        string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);
}
