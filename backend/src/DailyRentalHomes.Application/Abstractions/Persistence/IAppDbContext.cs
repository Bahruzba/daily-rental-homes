using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RentalHome> RentalHomes { get; }
    DbSet<MediaFile> MediaFiles { get; }
    DbSet<RelatedContact> RelatedContacts { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<BookingDate> BookingDates { get; }
    DbSet<BookingDeposit> BookingDeposits { get; }
    DbSet<BookingStatus> BookingStatuses { get; }
    DbSet<PaymentCard> PaymentCards { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
