namespace DailyRentalHomes.Api.Contracts.Amenities;

public sealed class AmenityInput
{
    public string Name { get; set; } = string.Empty;
    public string? IconName { get; set; }
}
