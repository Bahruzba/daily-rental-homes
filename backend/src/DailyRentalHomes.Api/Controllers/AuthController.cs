using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Auth;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Application.Abstractions.Messaging;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMessageSender _messageSender;
    private readonly AccessTokenBuilder _accessTokenBuilder;
    private readonly IHostEnvironment _environment;

    public AuthController(
        AppDbContext db,
        IMessageSender messageSender,
        AccessTokenBuilder accessTokenBuilder,
        IHostEnvironment environment)
    {
        _db = db;
        _messageSender = messageSender;
        _accessTokenBuilder = accessTokenBuilder;
        _environment = environment;
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [HttpPost("send")]
    public async Task<IActionResult> Send(PhoneInput input, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(input.Phone) || input.Phone.Trim().Length > 30)
        {
            return BadRequest(ApiResponse<object>.Fail("Phone is required and must not exceed 30 characters."));
        }

        var phone = TextRules.Clean(input.Phone);
        var pin = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var text = $"Daily Rental Homes PIN: {pin}";
        var providerId = await _messageSender.SendAsync(MessageChannel.WhatsApp, phone, text, cancellationToken);

        var item = new OtpCode
        {
            PhoneNumber = phone,
            CodeHash = Hash(pin),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        var message = new OutboundMessage
        {
            TypeCode = NotificationTypeCodes.OtpCode,
            Title = "Giriş kodu",
            Channel = MessageChannel.WhatsApp,
            Status = MessageStatus.Sent,
            To = phone,
            Text = text,
            ProviderMessageId = providerId,
            SentAt = DateTime.UtcNow
        };

        _db.OtpCodes.Add(item);
        _db.OutboundMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new OtpRequestResponse(
            "Code sent.",
            item.ExpiresAt,
            _environment.IsDevelopment() ? pin : null);

        return Ok(ApiResponse<OtpRequestResponse>.Ok(response));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(ConfirmInput input, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(input.Phone) || input.Phone.Trim().Length > 30 ||
            TextRules.Empty(input.Pin) || input.Pin.Length != 6 || !input.Pin.All(char.IsDigit))
        {
            return BadRequest(ApiResponse<object>.Fail("Phone and a valid 6-digit PIN are required."));
        }

        if (!TextRules.Empty(input.FullName) && input.FullName.Trim().Length > 150)
        {
            return BadRequest(ApiResponse<object>.Fail("Full name must not exceed 150 characters."));
        }

        var phone = TextRules.Clean(input.Phone);
        var otp = await _db.OtpCodes
            .Where(x => x.PhoneNumber == phone && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || otp.TryCount >= 5)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid code."));
        }

        if (!MatchesHash(input.Pin, otp.CodeHash))
        {
            otp.TryCount++;
            await _db.SaveChangesAsync(cancellationToken);
            return BadRequest(ApiResponse<object>.Fail("Invalid code."));
        }

        otp.UsedAt = DateTime.UtcNow;

        var user = await _db.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phone && !x.IsDeleted, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                FullName = TextRules.Empty(input.FullName) ? phone : TextRules.Clean(input.FullName),
                PhoneNumber = phone,
                Role = UserRole.Customer
            };
            _db.Users.Add(user);
        }

        if (!user.IsActive)
        {
            return Unauthorized(ApiResponse<object>.Fail("User is inactive."));
        }

        await _db.SaveChangesAsync(cancellationToken);

        var token = _accessTokenBuilder.Create(user);
        var response = new AuthSessionResponse(
            token.AccessToken,
            token.ExpiresAt,
            new AuthUserResponse(user.Id, user.FullName, user.PhoneNumber, user.Role.ToString()));

        return Ok(ApiResponse<AuthSessionResponse>.Ok(response));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            id = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            name = User.FindFirst("name")?.Value,
            phoneNumber = User.FindFirst("phone_number")?.Value,
            role = User.FindFirst("role")?.Value
        }));
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static bool MatchesHash(string value, string expectedHash)
    {
        try
        {
            var actualBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            var expectedBytes = Convert.FromHexString(expectedHash);
            return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
