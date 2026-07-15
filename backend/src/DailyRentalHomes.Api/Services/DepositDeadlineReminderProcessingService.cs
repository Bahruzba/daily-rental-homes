using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Services;

public interface IDepositDeadlineReminderProcessingService
{
    Task<DepositDeadlineReminderProcessingResult> ProcessAsync(CancellationToken cancellationToken);
}

public sealed record DepositDeadlineReminderProcessingResult(
    int Evaluated,
    int Eligible,
    int Queued,
    int DuplicateSkipped);

public sealed class DepositDeadlineReminderProcessingService : IDepositDeadlineReminderProcessingService
{
    private static readonly string[] BlockedBookingStatuses =
    [
        BookingStatusCodes.Cancelled,
        BookingStatusCodes.Completed,
        BookingStatusCodes.Rejected
    ];

    private readonly AppDbContext _db;
    private readonly INotificationOutboxService _notifications;
    private readonly DepositReminderOptions _options;

    public DepositDeadlineReminderProcessingService(
        AppDbContext db,
        INotificationOutboxService notifications,
        IOptions<DepositReminderOptions> options)
    {
        _db = db;
        _notifications = notifications;
        _options = options.Value;
    }

    public async Task<DepositDeadlineReminderProcessingResult> ProcessAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var reminderBeforeHours = Math.Max(1, _options.ReminderBeforeHours);
        var windowEnd = now.AddHours(reminderBeforeHours);

        var evaluated = await _db.BookingDeposits
            .AsNoTracking()
            .CountAsync(deposit =>
                !deposit.IsDeleted &&
                deposit.DeadlineAt.HasValue &&
                deposit.DeadlineAt.Value > now,
                cancellationToken);

        var deposits = await _db.BookingDeposits
            .Include(deposit => deposit.Booking)
                .ThenInclude(booking => booking!.Status)
            .Include(deposit => deposit.Booking)
                .ThenInclude(booking => booking!.RentalHome)
            .Where(deposit =>
                !deposit.IsDeleted &&
                deposit.DeadlineAt.HasValue &&
                deposit.DeadlineAt.Value > now &&
                deposit.DeadlineAt.Value <= windowEnd &&
                deposit.Status != BookingDepositStatus.Paid &&
                deposit.Booking != null &&
                !deposit.Booking.IsDeleted &&
                deposit.Booking.Status != null &&
                !BlockedBookingStatuses.Contains(deposit.Booking.Status.Code))
            .OrderBy(deposit => deposit.DeadlineAt)
            .ThenBy(deposit => deposit.Id)
            .ToListAsync(cancellationToken);

        var queued = 0;
        var duplicateSkipped = 0;

        foreach (var deposit in deposits)
        {
            if (await HasReminderForCurrentDeadline(deposit, cancellationToken))
            {
                duplicateSkipped++;
                continue;
            }

            await _notifications.QueueDepositDeadlineReminderAsync(deposit.Booking!, deposit, cancellationToken);
            queued++;
        }

        if (queued > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new DepositDeadlineReminderProcessingResult(
            evaluated,
            deposits.Count,
            queued,
            duplicateSkipped);
    }

    private Task<bool> HasReminderForCurrentDeadline(BookingDeposit deposit, CancellationToken cancellationToken)
    {
        var deadlineText = deposit.DeadlineAt!.Value.ToString("dd.MM.yyyy HH:mm");
        return _db.OutboundMessages
            .AsNoTracking()
            .AnyAsync(message =>
                !message.IsDeleted &&
                message.BookingDepositId == deposit.Id &&
                message.TypeCode == NotificationTypeCodes.DepositDeadlineReminder &&
                message.Text.Contains(deadlineText),
                cancellationToken);
    }
}
