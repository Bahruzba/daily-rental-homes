using DailyRentalHomes.Domain.Common;
using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Domain.Entities;

public sealed class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
