using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using Microsoft.Extensions.Options;
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
        var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
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
    }
}
