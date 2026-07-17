using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace DailyRentalHomes.Tests;

public sealed class AccessTokenBuilderTests
{
    [Fact]
    public void Create_ReturnsValidSignedTokenWithIdentityAndRoleClaims()
    {
        var options = new JwtOptions
        {
            Issuer = "tests",
            Audience = "test-clients",
            Key = "TEST_SIGNING_KEY_WITH_AT_LEAST_32_BYTES_123456",
            Minutes = 15
        };
        var builder = new AccessTokenBuilder(Options.Create(options));
        var user = new User
        {
            Id = 42,
            FullName = "Test Broker",
            PhoneNumber = "+994501112233",
            Role = UserRole.Broker
        };

        var token = builder.Create(user);
        var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = tokenHandler.ValidateToken(token.AccessToken, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "name",
            RoleClaimType = "role"
        }, out var validatedToken);

        Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal("42", principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal("Test Broker", principal.FindFirst("name")?.Value);
        Assert.Equal("+994501112233", principal.FindFirst("phone_number")?.Value);
        Assert.Equal(nameof(UserRole.Broker), principal.FindFirst("role")?.Value);
        Assert.True(token.ExpiresAt > DateTime.UtcNow.AddMinutes(14));
    }

    [Fact]
    public void JwtSecretValidator_AllowsDevelopmentPlaceholderOnlyInDevelopment()
    {
        const string developmentKey = "LOCAL_DEVELOPMENT_KEY_CHANGE_LATER_123456789";

        Assert.True(JwtSecretValidator.IsAllowed(developmentKey, Environments.Development));
        Assert.False(JwtSecretValidator.IsAllowed(developmentKey, Environments.Production));
    }

    [Fact]
    public void JwtSecretValidator_RejectsPlaceholderProductionSecretWithoutEchoingIt()
    {
        const string placeholder = "CHANGE_ME_TO_A_SECURE_32_BYTE_MINIMUM_SECRET";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            JwtSecretValidator.ThrowIfUnsafe(placeholder, Environments.Production));

        Assert.Contains("Token key", exception.Message);
        Assert.DoesNotContain(placeholder, exception.Message);
    }

    [Fact]
    public void JwtSecretValidator_AllowsUniqueProductionSecret()
    {
        const string uniqueSecret = "prod_secret_9f7c2c4f6a12422aa72f59b61e8d8c5b";

        Assert.True(JwtSecretValidator.IsAllowed(uniqueSecret, Environments.Production));
    }
}
