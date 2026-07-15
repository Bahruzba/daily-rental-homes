using System.Text.Json;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Services;

public sealed class MetaWhatsAppWebhookService
{
    private static readonly IReadOnlyDictionary<string, int> StatusRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["sent"] = 1,
        ["delivered"] = 2,
        ["read"] = 3,
        ["failed"] = 4
    };

    private readonly AppDbContext _db;
    private readonly ILogger<MetaWhatsAppWebhookService> _logger;

    public MetaWhatsAppWebhookService(AppDbContext db, ILogger<MetaWhatsAppWebhookService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MetaWhatsAppWebhookProcessResult> ProcessAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        var statuses = ExtractStatuses(payload).ToList();
        if (statuses.Count == 0)
        {
            return new MetaWhatsAppWebhookProcessResult(0, 0, 0);
        }

        var updated = 0;
        var ignored = 0;
        var unknown = 0;

        foreach (var status in statuses)
        {
            if (!StatusRanks.ContainsKey(status.Status))
            {
                ignored++;
                continue;
            }

            var message = await _db.OutboundMessages
                .FirstOrDefaultAsync(item => item.ProviderMessageId == status.ProviderMessageId, cancellationToken);

            if (message is null)
            {
                unknown++;
                _logger.LogInformation("Meta WhatsApp webhook status for unknown provider message id {ProviderMessageId} was ignored.", status.ProviderMessageId);
                continue;
            }

            if (!ShouldApplyStatus(message.ProviderDeliveryStatus, status.Status))
            {
                ignored++;
                continue;
            }

            ApplyStatus(message, status);
            updated++;
        }

        if (updated > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new MetaWhatsAppWebhookProcessResult(statuses.Count, updated, ignored + unknown);
    }

    private static IEnumerable<MetaWhatsAppStatusEvent> ExtractStatuses(JsonElement payload)
    {
        if (!payload.TryGetProperty("entry", out var entries) || entries.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value) ||
                    !value.TryGetProperty("statuses", out var statuses) ||
                    statuses.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var status in statuses.EnumerateArray())
                {
                    var providerMessageId = GetString(status, "id");
                    var statusCode = GetString(status, "status");
                    if (string.IsNullOrWhiteSpace(providerMessageId) || string.IsNullOrWhiteSpace(statusCode))
                    {
                        continue;
                    }

                    yield return new MetaWhatsAppStatusEvent(
                        providerMessageId,
                        statusCode.Trim().ToLowerInvariant(),
                        GetTimestamp(status),
                        GetErrorMessage(status));
                }
            }
        }
    }

    private static bool ShouldApplyStatus(string? currentStatus, string nextStatus)
    {
        if (string.IsNullOrWhiteSpace(currentStatus)) return true;
        return StatusRanks.GetValueOrDefault(nextStatus) >= StatusRanks.GetValueOrDefault(currentStatus);
    }

    private static void ApplyStatus(OutboundMessage message, MetaWhatsAppStatusEvent status)
    {
        var occurredAt = status.Timestamp ?? DateTime.UtcNow;
        message.ProviderDeliveryStatus = status.Status;
        message.ProviderStatusUpdatedAt = occurredAt;

        switch (status.Status)
        {
            case "sent":
                message.Status = MessageStatus.Sent;
                message.SentAt ??= occurredAt;
                break;
            case "delivered":
                message.Status = MessageStatus.Sent;
                message.DeliveredAt ??= occurredAt;
                break;
            case "read":
                message.Status = MessageStatus.Sent;
                message.DeliveredAt ??= occurredAt;
                message.ReadAt ??= occurredAt;
                break;
            case "failed":
                message.Status = MessageStatus.Failed;
                message.ErrorMessage = Truncate(status.ErrorMessage ?? "Meta WhatsApp delivery failed.", 1000);
                break;
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static DateTime? GetTimestamp(JsonElement element)
    {
        var timestamp = GetString(element, "timestamp");
        return long.TryParse(timestamp, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : null;
    }

    private static string? GetErrorMessage(JsonElement element)
    {
        if (!element.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Array || errors.GetArrayLength() == 0)
        {
            return null;
        }

        var first = errors[0];
        var parts = new List<string>();
        if (first.TryGetProperty("code", out var code) && code.TryGetInt32(out var codeValue)) parts.Add($"Code: {codeValue}.");
        var title = GetString(first, "title");
        if (!string.IsNullOrWhiteSpace(title)) parts.Add($"Title: {title}.");
        var message = GetString(first, "message");
        if (!string.IsNullOrWhiteSpace(message)) parts.Add($"Message: {message}.");
        return parts.Count == 0 ? null : string.Join(' ', parts);
    }

    private static string Truncate(string value, int maxLength) =>
        value[..Math.Min(value.Length, maxLength)];

    private sealed record MetaWhatsAppStatusEvent(
        string ProviderMessageId,
        string Status,
        DateTime? Timestamp,
        string? ErrorMessage);
}

public sealed record MetaWhatsAppWebhookProcessResult(int Received, int Updated, int Ignored);
