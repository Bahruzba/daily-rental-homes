using DailyRentalHomes.Api.Contracts.Deposits;
using DailyRentalHomes.Application.Abstractions.Messaging;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/deposits")]
public sealed class DepositsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMessageSender _messageSender;

    public DepositsController(AppDbContext db, IMessageSender messageSender)
    {
        _db = db;
        _messageSender = messageSender;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.BookingDeposits.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await _db.BookingDeposits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewDepositRequest request, CancellationToken cancellationToken)
    {
        var deposit = new BookingDeposit
        {
            BookingId = request.BookingId,
            Amount = request.Amount,
            DeadlineAt = request.DeadlineAt,
            PaymentCardId = request.PaymentCardId,
            Note = request.Note,
            Status = BookingDepositStatus.Waiting
        };

        _db.BookingDeposits.Add(deposit);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(deposit.Id);
    }

    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, UpdateDepositStatusInput input, CancellationToken cancellationToken)
    {
        var deposit = await _db.BookingDeposits.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (deposit is null)
        {
            return NotFound();
        }

        deposit.Status = input.Status;
        deposit.Note = input.Note ?? deposit.Note;

        if (input.Status == BookingDepositStatus.Paid)
        {
            deposit.PaidAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpPost("{id:long}/reminder")]
    public async Task<IActionResult> SendReminder(long id, SendDepositReminderInput input, CancellationToken cancellationToken)
    {
        var deposit = await _db.BookingDeposits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (deposit is null)
        {
            return NotFound();
        }

        var text = input.CustomText ?? $"Beh odenisi ucun mebleg: {deposit.Amount} AZN. Zehmet olmasa vaxtinda gonderin.";
        var providerId = await _messageSender.SendAsync(MessageChannel.WhatsApp, input.To, text, cancellationToken);

        var message = new OutboundMessage
        {
            Channel = MessageChannel.WhatsApp,
            Status = MessageStatus.Sent,
            To = input.To,
            Text = text,
            ProviderMessageId = providerId,
            SentAt = DateTime.UtcNow,
            BookingId = deposit.BookingId,
            BookingDepositId = deposit.Id
        };

        _db.OutboundMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(message.Id);
    }
}
