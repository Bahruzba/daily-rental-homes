namespace DailyRentalHomes.Api.Contracts.Auth;

public sealed class ConfirmInput
{
    public string Phone { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Role { get; set; } = 3;
}
