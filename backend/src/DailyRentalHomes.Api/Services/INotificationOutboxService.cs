using DailyRentalHomes.Domain.Entities;

namespace DailyRentalHomes.Api.Services;

public interface INotificationOutboxService
{
    Task QueueBookingCreatedAsync(Booking booking, RentalHome home, CancellationToken cancellationToken);
    Task QueueDepositRequestedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken);
    Task QueueDepositReceiptUploadedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken);
    Task QueueDepositApprovedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken);
    Task QueueDepositRejectedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken);
    Task QueueBookingStatusChangedAsync(Booking booking, string statusCode, CancellationToken cancellationToken);
}
