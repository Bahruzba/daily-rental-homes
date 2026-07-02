using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Infrastructure.Persistence;

public static class DbSeed
{
    public static async Task RunAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.BookingStatuses.AnyAsync(cancellationToken))
        {
            db.BookingStatuses.Add(new BookingStatus { Name = "Pending", Code = BookingStatusCodes.Pending, SortOrder = 1 });
            db.BookingStatuses.Add(new BookingStatus { Name = "WaitingDeposit", Code = BookingStatusCodes.WaitingDeposit, SortOrder = 2 });
            db.BookingStatuses.Add(new BookingStatus { Name = "Paid", Code = BookingStatusCodes.Paid, SortOrder = 3 });
            db.BookingStatuses.Add(new BookingStatus { Name = "Confirmed", Code = BookingStatusCodes.Confirmed, SortOrder = 4 });
            db.BookingStatuses.Add(new BookingStatus { Name = "Cancelled", Code = BookingStatusCodes.Cancelled, SortOrder = 5 });
            db.BookingStatuses.Add(new BookingStatus { Name = "Completed", Code = BookingStatusCodes.Completed, SortOrder = 6 });
        }

        if (!await db.Amenities.AnyAsync(cancellationToken))
        {
            db.Amenities.Add(new Amenity { Name = "Pool", IconName = "pool" });
            db.Amenities.Add(new Amenity { Name = "BBQ", IconName = "bbq" });
            db.Amenities.Add(new Amenity { Name = "Yard", IconName = "yard" });
            db.Amenities.Add(new Amenity { Name = "WiFi", IconName = "wifi" });
            db.Amenities.Add(new Amenity { Name = "Parking", IconName = "parking" });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
