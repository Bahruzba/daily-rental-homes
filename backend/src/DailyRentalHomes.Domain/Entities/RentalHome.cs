using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class RentalHome : BaseEntity
{
    public long BrokerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string? Address { get; set; }
    public decimal DailyPrice { get; set; }
    public int RoomCount { get; set; }
    public int GuestCount { get; set; }
    public bool IsPublished { get; set; }

    public User? BrokerUser { get; set; }
    public ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();
    public ICollection<RelatedContact> Contacts { get; set; } = new List<RelatedContact>();
    public ICollection<RentalHomeAmenity> Amenities { get; set; } = new List<RentalHomeAmenity>();
}
