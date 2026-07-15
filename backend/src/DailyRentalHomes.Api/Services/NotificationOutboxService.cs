using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DailyRentalHomes.Api.Services;

public sealed class NotificationOutboxService : INotificationOutboxService
{
    private readonly AppDbContext _db;

    public NotificationOutboxService(AppDbContext db) => _db = db;

    public async Task QueueBookingCreatedAsync(Booking booking, RentalHome home, CancellationToken cancellationToken)
    {
        var broker = await ActiveUser(home.BrokerUserId, cancellationToken);
        if (broker is null) return;
        Queue(broker.Id, broker.FullName, broker.PhoneNumber, NotificationTypeCodes.BookingCreated,
            "Yeni rezervasiya", $"{home.Title} üçün yeni rezervasiya #{booking.Id} yaradıldı.", booking, null, DateTime.UtcNow);
    }

    public async Task QueueBookingCancellationRequestedAsync(Booking booking, BookingCancellationRequest request, CancellationToken cancellationToken)
    {
        var broker = await BrokerFor(booking, cancellationToken);
        if (broker is null) return;
        Queue(broker.Id, broker.FullName, broker.PhoneNumber, NotificationTypeCodes.BookingCancellationRequested,
            "Rezervasiya cancel request",
            $"Booking #{booking.Id} cancellation requested by {booking.CustomerFullName}, phone: {booking.CustomerPhoneNumber}.",
            booking, booking.Deposit, DateTime.UtcNow);
    }

    public Task QueueBookingCancellationApprovedAsync(Booking booking, BookingCancellationRequest request, CancellationToken cancellationToken)
    {
        Queue(booking.CustomerUserId, booking.CustomerFullName, booking.CustomerPhoneNumber,
            NotificationTypeCodes.BookingCancellationApproved,
            "LÉ™ÄŸv sorÄŸusu tÉ™sdiqlÉ™ndi",
            $"Rezervasiya #{booking.Id} Ã¼zrÉ™ lÉ™ÄŸv sorÄŸunuz tÉ™sdiqlÉ™ndi vÉ™ rezervasiya lÉ™ÄŸv edildi.",
            booking, booking.Deposit, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task QueueBookingCancellationRejectedAsync(Booking booking, BookingCancellationRequest request, CancellationToken cancellationToken)
    {
        var note = string.IsNullOrWhiteSpace(request.DecisionNote) ? string.Empty : $" Broker qeydi: {request.DecisionNote}";
        Queue(booking.CustomerUserId, booking.CustomerFullName, booking.CustomerPhoneNumber,
            NotificationTypeCodes.BookingCancellationRejected,
            "LÉ™ÄŸv sorÄŸusu rÉ™dd edildi",
            $"Rezervasiya #{booking.Id} Ã¼zrÉ™ lÉ™ÄŸv sorÄŸunuz rÉ™dd edildi.{note}",
            booking, booking.Deposit, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task QueueDepositRequestedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken)
    {
        Queue(booking.CustomerUserId, booking.CustomerFullName, booking.CustomerPhoneNumber,
            NotificationTypeCodes.DepositRequested, "Beh tələbi",
            $"Rezervasiya #{booking.Id} üçün {deposit.Amount:0.##} AZN beh tələb edildi. Son tarix: {deposit.DeadlineAt:dd.MM.yyyy HH:mm}.",
            booking, deposit, DateTime.UtcNow);

        var reminderAt = GetReminderAt(deposit.DeadlineAt, DateTime.UtcNow);
        if (reminderAt.HasValue)
        {
            Queue(booking.CustomerUserId, booking.CustomerFullName, booking.CustomerPhoneNumber,
                NotificationTypeCodes.DepositDeadlineReminder, "Beh üçün son tarix yaxınlaşır",
                $"Rezervasiya #{booking.Id} üzrə beh ödənişinin son tarixi {deposit.DeadlineAt:dd.MM.yyyy HH:mm}-dır.",
                booking, deposit, reminderAt.Value, DepositPayload(booking, deposit, NotificationTypeCodes.DepositDeadlineReminder));
        }

        return Task.CompletedTask;
    }

    public Task QueueDepositDeadlineReminderAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken)
    {
        Queue(booking.CustomerUserId, booking.CustomerFullName, booking.CustomerPhoneNumber,
            NotificationTypeCodes.DepositDeadlineReminder, "Beh üçün son tarix yaxınlaşır",
            $"Rezervasiya #{booking.Id} üzrə beh ödənişinin son tarixi {deposit.DeadlineAt:dd.MM.yyyy HH:mm}-dır.",
            booking, deposit, DateTime.UtcNow, DepositPayload(booking, deposit, NotificationTypeCodes.DepositDeadlineReminder));
        return Task.CompletedTask;
    }

    public Task QueueDepositDeadlineExtendedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken)
    {
        var message = $"Rezervasiya üçün beh göndərmə müddəti {deposit.DeadlineAt:dd.MM.yyyy HH:mm} tarixinədək uzadıldı.";
        if (!string.IsNullOrWhiteSpace(deposit.DeadlineExtensionReason))
        {
            message += $" Səbəb: {deposit.DeadlineExtensionReason}";
        }

        Queue(booking.CustomerUserId, booking.CustomerFullName, booking.CustomerPhoneNumber,
            NotificationTypeCodes.DepositDeadlineExtended, "Beh müddəti uzadıldı",
            message, booking, deposit, DateTime.UtcNow, DepositPayload(booking, deposit, NotificationTypeCodes.DepositDeadlineExtended));
        return Task.CompletedTask;
    }

    public async Task QueueDepositReceiptUploadedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken)
    {
        var broker = await BrokerFor(booking, cancellationToken);
        if (broker is null) return;
        Queue(broker.Id, broker.FullName, broker.PhoneNumber, NotificationTypeCodes.DepositReceiptUploaded,
            "Beh qəbzi yükləndi", $"Müştəri rezervasiya #{booking.Id} üçün beh qəbzi yüklədi.", booking, deposit, DateTime.UtcNow);
    }

    public Task QueueDepositApprovedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken)
    {
        Queue(booking.CustomerUserId, booking.CustomerFullName, booking.CustomerPhoneNumber,
            NotificationTypeCodes.DepositApproved, "Beh təsdiqləndi",
            $"Rezervasiya #{booking.Id} üzrə beh təsdiqləndi və rezervasiya qüvvəyə mindi.", booking, deposit, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task QueueDepositRejectedAsync(Booking booking, BookingDeposit deposit, CancellationToken cancellationToken)
    {
        Queue(booking.CustomerUserId, booking.CustomerFullName, booking.CustomerPhoneNumber,
            NotificationTypeCodes.DepositRejected, "Beh qəbzi rədd edildi",
            $"Rezervasiya #{booking.Id} üzrə beh qəbzi rədd edildi. Zəhmət olmasa məlumatları yoxlayın.", booking, deposit, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task QueueBookingStatusChangedAsync(Booking booking, string statusCode, CancellationToken cancellationToken)
    {
        Queue(booking.CustomerUserId, booking.CustomerFullName, booking.CustomerPhoneNumber,
            NotificationTypeCodes.BookingStatusChanged, "Rezervasiya statusu dəyişdi",
            $"Rezervasiya #{booking.Id} statusu '{statusCode}' olaraq dəyişdirildi.", booking, booking.Deposit, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public static DateTime? GetReminderAt(DateTime? deadlineAt, DateTime now)
    {
        if (!deadlineAt.HasValue) return null;
        var remaining = deadlineAt.Value - now;
        if (remaining > TimeSpan.FromHours(3)) return deadlineAt.Value.AddHours(-2);
        if (remaining > TimeSpan.FromMinutes(30)) return deadlineAt.Value.AddMinutes(-30);
        return null;
    }

    private async Task<User?> BrokerFor(Booking booking, CancellationToken cancellationToken)
    {
        var brokerId = booking.RentalHome?.BrokerUserId ?? await _db.RentalHomes
            .Where(home => home.Id == booking.RentalHomeId)
            .Select(home => home.BrokerUserId)
            .SingleAsync(cancellationToken);
        return await ActiveUser(brokerId, cancellationToken);
    }

    private Task<User?> ActiveUser(long userId, CancellationToken cancellationToken) => _db.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(user => user.Id == userId && user.IsActive, cancellationToken);

    private void Queue(long? userId, string? name, string phone, string type, string title, string message,
        Booking booking, BookingDeposit? deposit, DateTime scheduledAt, object? payload = null)
    {
        if (string.IsNullOrWhiteSpace(phone)) return;
        _db.OutboundMessages.Add(new OutboundMessage
        {
            RecipientUserId = userId,
            RecipientName = name,
            Channel = MessageChannel.WhatsApp,
            Status = MessageStatus.Pending,
            TypeCode = type,
            Title = title,
            To = phone.Trim(),
            Text = message,
            ScheduledAt = scheduledAt,
            Booking = booking,
            BookingDeposit = deposit,
            PayloadJson = JsonSerializer.Serialize(payload ?? new { bookingId = booking.Id, depositId = deposit?.Id, type })
        });
    }

    private static object DepositPayload(Booking booking, BookingDeposit deposit, string type) => new
    {
        bookingId = booking.Id,
        depositId = deposit.Id,
        type,
        deadlineAt = deposit.DeadlineAt,
        deadlineText = deposit.DeadlineAt?.ToString("dd.MM.yyyy HH:mm"),
        deadlineExtensionReason = deposit.DeadlineExtensionReason
    };
}
