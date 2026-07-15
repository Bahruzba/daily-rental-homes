using DailyRentalHomes.Domain.Entities;

namespace DailyRentalHomes.Api.Services;

public interface INotificationOutboxService
{
    Task QueueBookingCreatedAsync(Booking booking, RentalHome home, CancellationToken cancellationToken);
    Task QueueBookingCancellationRequestedAsync(Booking booking, BookingCancellationRequest request, CancellationToken cancellationToken);
    Task QueueBookingCancellationApprovedAsync(Booking booking, BookingCancellationRequest request, CancellationToken cancellationToken);
    Task QueueBookingCancellationRejectedAsync(Booking booking, BookingCancellationRequest request, CancellationToken cancellationToken);
    Task QueueDepositRequestedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken);
    Task QueueDepositDeadlineReminderAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken);
    Task QueueDepositDeadlineExtendedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken);
    Task QueueDepositReceiptUploadedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken);
    Task QueueDepositApprovedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken);
    Task QueueDepositRejectedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken);
    Task QueueBookingStatusChangedAsync(Booking booking, string statusCode, CancellationToken cancellationToken);
}
