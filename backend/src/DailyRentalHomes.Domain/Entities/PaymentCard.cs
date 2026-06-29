using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class PaymentCard : BaseEntity
{
    public long BrokerUserId { get; set; }
    public string CardHolderName { get; set; } = string.Empty;
    public string PanMasked { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public User? BrokerUser { get; set; }
}
