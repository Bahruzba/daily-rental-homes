using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Tests;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ReturnsSubjectClaim()
    {
        var principal = CreatePrincipal("27", UserRole.Broker);

        Assert.Equal(27, principal.GetUserId());
    }

    [Fact]
    public void IsAdmin_UsesRoleClaim()
    {
        var admin = CreatePrincipal("1", UserRole.Admin);
        var broker = CreatePrincipal("2", UserRole.Broker);

        Assert.True(admin.IsAdmin());
        Assert.False(broker.IsAdmin());
    }

    [Fact]
    public void GetUserId_RejectsMissingSubjectClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, UserRole role)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("role", role.ToString())
            },
            authenticationType: "test",
            nameType: "name",
            roleType: "role");

        return new ClaimsPrincipal(identity);
    }
}
