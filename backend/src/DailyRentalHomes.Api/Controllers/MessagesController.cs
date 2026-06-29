using DailyRentalHomes.Api.Contracts.Messages;
using DailyRentalHomes.Application.Abstractions.Messaging;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
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
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Send(SendMessageRequest request, CancellationToken cancellationToken)
    {
        var providerId = await _messageSender.SendAsync(request.Channel, request.To, request.Text, cancellationToken);
        var item = new OutboundMessage
        {
            Channel = request.Channel,
            Status = MessageStatus.Sent,
            To = request.To,
            Text = request.Text,
            ProviderMessageId = providerId,
            SentAt = DateTime.UtcNow,
            BookingId = request.BookingId,
            BookingDepositId = request.BookingDepositId
        };

        _db.OutboundMessages.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(item.Id);
    }
}
