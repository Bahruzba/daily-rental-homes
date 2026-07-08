namespace DailyRentalHomes.Api.Contracts.RentalHomes;

public sealed class PublicRentalHomeSearchRequest
{
    public string? City { get; set; }
    public string? District { get; set; }
    public int? Guests { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Q { get; set; }
}
