using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Api.Contracts.Contacts;

public sealed class NewContactRequest
{
    public long RentalHomeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public ContactType ContactType { get; set; }
    public bool NotifyEnabled { get; set; }
}
