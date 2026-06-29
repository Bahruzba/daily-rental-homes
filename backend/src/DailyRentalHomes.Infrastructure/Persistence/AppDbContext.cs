using DailyRentalHomes.Application.Abstractions.Persistence;
using DailyRentalHomes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<RentalHome> RentalHomes => Set<RentalHome>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<RelatedContact> RelatedContacts => Set<RelatedContact>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<RentalHomeAmenity> RentalHomeAmenities => Set<RentalHomeAmenity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingDate> BookingDates => Set<BookingDate>();
    public DbSet<BookingDeposit> BookingDeposits => Set<BookingDeposit>();
    public DbSet<BookingStatus> BookingStatuses => Set<BookingStatus>();
    public DbSet<BookingStatusHistory> BookingStatusHistory => Set<BookingStatusHistory>();
    public DbSet<PaymentCard> PaymentCards => Set<PaymentCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
