using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Services;

public sealed class NotificationDeliveryService
{
    private const int MaxBatchSize = 100;
    private readonly AppDbContext _db;
    private readonly INotificationDeliveryProvider _provider;
    private readonly NotificationRetryOptions _retryOptions;
    private readonly ILogger<NotificationDeliveryService> _logger;

    public NotificationDeliveryService(
        AppDbContext db,
        INotificationDeliveryProvider provider,
        IOptions<NotificationDeliveryOptions>? options = null,
        ILogger<NotificationDeliveryService>? logger = null)
    {
        _db = db;
        _provider = provider;
        _retryOptions = options?.Value.Retry ?? new NotificationRetryOptions();
        _logger = logger ?? NullLogger<NotificationDeliveryService>.Instance;
    }

    public async Task<NotificationDeliverySummary> ProcessPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var normalizedBatchSize = Math.Clamp(batchSize, 1, MaxBatchSize);
        var now = DateTime.UtcNow;
        var messages = await _db.OutboundMessages
            .Where(message => message.Status == MessageStatus.Pending &&
                              (!message.ScheduledAt.HasValue || message.ScheduledAt <= now) &&
                              (!message.NextAttemptAt.HasValue || message.NextAttemptAt <= now))
            .OrderBy(message => message.ScheduledAt ?? message.CreatedAt)
            .ThenBy(message => message.NextAttemptAt ?? message.CreatedAt)
            .ThenBy(message => message.Id)
            .Take(normalizedBatchSize)
            .ToListAsync(cancellationToken);

        var summary = await ProcessMessagesAsync(messages, now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return summary;
    }

    public async Task<NotificationDeliverySummary?> RetryMessageAsync(long messageId, CancellationToken cancellationToken)
    {
        var message = await _db.OutboundMessages
            .FirstOrDefaultAsync(item => item.Id == messageId, cancellationToken);

        if (message is null)
        {
            return null;
        }

        message.Status = MessageStatus.Pending;
        message.DeliveryAttemptCount = 0;
        message.LastAttemptAt = null;
        message.NextAttemptAt = null;
        message.ErrorMessage = null;
        message.ProviderMessageId = null;
        message.SentAt = null;

        var summary = await ProcessMessagesAsync([message], DateTime.UtcNow, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return summary;
    }

    internal static DateTime CalculateNextAttemptAt(DateTime now, int attemptCount, NotificationRetryOptions options)
    {
        var safeAttemptCount = Math.Max(1, attemptCount);
        var initialDelay = Math.Max(1, options.InitialDelayMinutes);
        var maxDelay = Math.Max(initialDelay, options.MaxDelayMinutes);
        var multiplier = Math.Pow(2, safeAttemptCount - 1);
        var delay = Math.Min(maxDelay, initialDelay * multiplier);
        return now.AddMinutes(delay);
    }

    private async Task<NotificationDeliverySummary> ProcessMessagesAsync(
        IReadOnlyList<DailyRentalHomes.Domain.Entities.OutboundMessage> messages,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var sent = 0;
        var failed = 0;
        var retried = 0;

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
                result = NotificationDeliveryResult.RetryableFailed("Notification provider threw an unexpected error.");
            }

            message.DeliveryAttemptCount++;
            message.LastAttemptAt = now;

            if (result.Success)
            {
                message.Status = MessageStatus.Sent;
                message.SentAt = DateTime.UtcNow;
                message.ProviderMessageId = Truncate(result.ProviderMessageId, 200);
                message.ErrorMessage = null;
                message.NextAttemptAt = null;
                sent++;
            }
            else if (result.IsRetryable && message.DeliveryAttemptCount < GetMaxAttempts())
            {
                message.Status = MessageStatus.Pending;
                message.ErrorMessage = Truncate(result.ErrorMessage ?? "Notification delivery failed.", 1000);
                message.NextAttemptAt = CalculateNextAttemptAt(now, message.DeliveryAttemptCount, _retryOptions);
                retried++;
                failed++;
            }
            else
            {
                message.Status = MessageStatus.Failed;
                message.ErrorMessage = Truncate(result.ErrorMessage ?? "Notification delivery failed.", 1000);
                message.NextAttemptAt = null;
                failed++;
            }
        }

        await Task.CompletedTask;
        return new NotificationDeliverySummary(messages.Count, sent, failed, retried);
    }

    private int GetMaxAttempts() => Math.Max(1, _retryOptions.MaxAttempts);

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, maxLength)];
}

public sealed record NotificationDeliverySummary(int Processed, int Sent, int Failed, int Retried);
