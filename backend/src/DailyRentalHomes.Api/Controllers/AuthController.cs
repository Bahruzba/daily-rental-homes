using DailyRentalHomes.Api.Contracts.Auth;
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

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send(PhoneInput input, CancellationToken cancellationToken)
    {
        var pin = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var item = new OtpCode
        {
            PhoneNumber = input.Phone,
            CodeHash = Hash(pin),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _db.OtpCodes.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { devPin = pin });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(ConfirmInput input, CancellationToken cancellationToken)
    {
        var pinHash = Hash(input.Pin);
        var otp = await _db.OtpCodes
            .Where(x => x.PhoneNumber == input.Phone && x.CodeHash == pinHash && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
        {
            return BadRequest("Invalid code");
        }

        otp.UsedAt = DateTime.UtcNow;

        var user = await _db.Users.FirstOrDefaultAsync(x => x.PhoneNumber == input.Phone, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                FullName = input.FullName,
                PhoneNumber = input.Phone,
                Role = (UserRole)input.Role
            };
            _db.Users.Add(user);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Id}:{user.PhoneNumber}:{user.Role}"));
        return Ok(new { user.Id, user.FullName, user.PhoneNumber, user.Role, token });
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
