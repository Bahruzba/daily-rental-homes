using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class DistributedLock : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
