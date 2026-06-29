using DailyRentalHomes.Domain.Entities;

namespace DailyRentalHomes.Application.Abstractions.Auth;

public interface ITokenService
{
    string CreateAccessToken(User user);
}
