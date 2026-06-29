namespace DailyRentalHomes.Application.Abstractions.Otp;

public interface IOtpGenerator
{
    string Generate();
    string Hash(string value);
    bool Matches(string value, string hash);
}
