namespace DailyRentalHomes.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Token";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int Minutes { get; set; } = 1440;
}
