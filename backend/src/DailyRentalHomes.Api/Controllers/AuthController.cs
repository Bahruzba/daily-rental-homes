using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Auth;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Application.Abstractions.Messaging;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public AuthController(AppDbContext db, IMessageSender messageSender, AccessTokenBuilder accessTokenBuilder)
    {
        _db = db;
        _messageSender = messageSender;
        _accessTokenBuilder = accessTokenBuilder;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send(PhoneInput input, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(input.Phone))
        {
            return BadRequest(ApiResponse<object>.Fail("Phone is required."));
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

        return Ok(ApiResponse<object>.Ok(new { devPin = pin }));
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(ConfirmInput input, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(input.Phone) || TextRules.Empty(input.Pin))
        {
            return BadRequest(ApiResponse<object>.Fail("Phone and PIN are required."));
        }

        var phone = TextRules.Clean(input.Phone);
        var pinHash = Hash(input.Pin);
        var otp = await _db.OtpCodes
            .Where(x => x.PhoneNumber == phone && x.CodeHash == pinHash && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid code."));
        }

        otp.UsedAt = DateTime.UtcNow;

        var user = await _db.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phone, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                FullName = TextRules.Empty(input.FullName) ? phone : TextRules.Clean(input.FullName),
                PhoneNumber = phone,
                Role = (UserRole)input.Role
            };
            _db.Users.Add(user);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var token = _accessTokenBuilder.Create(user);
        return Ok(ApiResponse<object>.Ok(new { user.Id, user.FullName, user.PhoneNumber, user.Role, token }));
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
