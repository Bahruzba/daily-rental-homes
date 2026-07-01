using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Messages;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Application.Abstractions.Messaging;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
[Route("api/messages")]
public sealed class MessagesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMessageSender _messageSender;

    public MessagesController(AppDbContext db, IMessageSender messageSender)
    {
        _db = db;
        _messageSender = messageSender;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.OutboundMessages.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPost]
    public async Task<IActionResult> Send(SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(request.To) || TextRules.Empty(request.Text))
        {
            return BadRequest(ApiResponse<object>.Fail("Receiver and text are required."));
        }

        var to = TextRules.Clean(request.To);
        var text = TextRules.Clean(request.Text);
        var providerId = await _messageSender.SendAsync(request.Channel, to, text, cancellationToken);
        var item = new OutboundMessage
        {
            Channel = request.Channel,
            Status = MessageStatus.Sent,
            To = to,
            Text = text,
            ProviderMessageId = providerId,
            SentAt = DateTime.UtcNow,
            BookingId = request.BookingId,
            BookingDepositId = request.BookingDepositId
        };

        _db.OutboundMessages.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { item.Id }));
    }
}
