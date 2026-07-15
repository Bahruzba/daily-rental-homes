using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DailyRentalHomes.Api.Services;

public sealed class NotificationDeliveryService
{
    private const int MaxBatchSize = 100;
    private readonly AppDbContext _db;
    private readonly INotificationDeliveryProvider _provider;
    private readonly ILogger<NotificationDeliveryService> _logger;

    public NotificationDeliveryService(
        AppDbContext db,
        INotificationDeliveryProvider provider,
        ILogger<NotificationDeliveryService>? logger = null)
    {
        _db = db;
        _provider = provider;
        _logger = logger ?? NullLogger<NotificationDeliveryService>.Instance;
    }

    public async Task<NotificationDeliverySummary> ProcessPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var normalizedBatchSize = Math.Clamp(batchSize, 1, MaxBatchSize);
        var now = DateTime.UtcNow;
        var messages = await _db.OutboundMessages
            .Where(message => message.Status == MessageStatus.Pending &&
                              (!message.ScheduledAt.HasValue || message.ScheduledAt <= now))
            .OrderBy(message => message.ScheduledAt ?? message.CreatedAt)
            .ThenBy(message => message.Id)
            .Take(normalizedBatchSize)
            .ToListAsync(cancellationToken);

        var sent = 0;
        var failed = 0;

        foreach (var message in messages)
        {
            NotificationDeliveryResult result;
            try
            {
                result = await _provider.SendAsync(message, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Notification provider failed while processing outbound message {OutboundMessageId}.", message.Id);
                result = NotificationDeliveryResult.Failed("Notification provider threw an unexpected error.");
            }

            if (result.Success)
            {
                message.Status = MessageStatus.Sent;
                message.SentAt = DateTime.UtcNow;
                message.ProviderMessageId = Truncate(result.ProviderMessageId, 200);
                message.ErrorMessage = null;
                sent++;
            }
            else
            {
                message.Status = MessageStatus.Failed;
                message.ErrorMessage = Truncate(result.ErrorMessage ?? "Notification delivery failed.", 1000);
                failed++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new NotificationDeliverySummary(messages.Count, sent, failed);
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, maxLength)];
}

public sealed record NotificationDeliverySummary(int Processed, int Sent, int Failed);
