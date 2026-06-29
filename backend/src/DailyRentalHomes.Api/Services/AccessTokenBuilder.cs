using DailyRentalHomes.Domain.Entities;
using System.Text;

namespace DailyRentalHomes.Api.Services;

public sealed class AccessTokenBuilder
{
    public string Create(User user)
    {
        var value = $"{user.Id}:{user.PhoneNumber}:{user.Role}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }
}
