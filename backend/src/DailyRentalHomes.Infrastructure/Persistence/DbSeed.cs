using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Infrastructure.Persistence;

public static class DbSeed
{
    public static async Task RunAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureBookingStatus(db, "Pending", BookingStatusCodes.Pending, 1, cancellationToken);
        await EnsureBookingStatus(db, "WaitingDeposit", BookingStatusCodes.WaitingDeposit, 2, cancellationToken);
        await EnsureBookingStatus(db, "Paid", BookingStatusCodes.Paid, 3, cancellationToken);
        await EnsureBookingStatus(db, "Confirmed", BookingStatusCodes.Confirmed, 4, cancellationToken);
        await EnsureBookingStatus(db, "Cancelled", BookingStatusCodes.Cancelled, 5, cancellationToken);
        await EnsureBookingStatus(db, "Rejected", BookingStatusCodes.Rejected, 6, cancellationToken);
        await EnsureBookingStatus(db, "Completed", BookingStatusCodes.Completed, 7, cancellationToken);

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

    private static async Task EnsureBookingStatus(
        AppDbContext db,
        string name,
        string code,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        if (await db.BookingStatuses.AnyAsync(status => status.Code == code, cancellationToken))
        {
            return;
        }

        db.BookingStatuses.Add(new BookingStatus { Name = name, Code = code, SortOrder = sortOrder });
    }
}
