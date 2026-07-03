using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Notifications;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/admin/notifications")]
public sealed class AdminNotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminNotificationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? status,
        [FromQuery] string? type,
        [FromQuery] long? bookingId,
        CancellationToken cancellationToken)
    {
        var query = _db.OutboundMessages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryStatus(status, out var parsedStatus))
            {
                return BadRequest(ApiResponse<object>.Fail("Notification status is not valid."));
            }
            query = query.Where(item => item.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var typeCode = type.Trim().ToLowerInvariant();
            query = query.Where(item => item.TypeCode == typeCode);
        }

        if (bookingId.HasValue) query = query.Where(item => item.BookingId == bookingId.Value);

        var entities = await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(500)
            .ToListAsync(cancellationToken);
        var items = entities.Select(item => new NotificationOutboxResponse(
                item.Id,
                item.TypeCode,
                NotificationChannelCodes.From(item.Channel),
                NotificationStatusCodes.From(item.Status),
                item.RecipientUserId,
                item.RecipientName,
                item.To,
                item.Title,
                item.Text,
                item.ScheduledAt,
                item.SentAt,
                item.BookingId,
                item.BookingDepositId,
                item.CreatedAt))
            .ToList();

        return Ok(ApiResponse<IReadOnlyList<NotificationOutboxResponse>>.Ok(items));
    }

    private static bool TryStatus(string value, out MessageStatus status)
    {
        status = value.Trim().ToLowerInvariant() switch
        {
            NotificationStatusCodes.Pending => MessageStatus.Pending,
            NotificationStatusCodes.Sent => MessageStatus.Sent,
            NotificationStatusCodes.Failed => MessageStatus.Failed,
            NotificationStatusCodes.Cancelled => MessageStatus.Cancelled,
            NotificationStatusCodes.Skipped => MessageStatus.Skipped,
            _ => 0
        };
        return status != 0;
    }
}
