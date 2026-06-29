using DailyRentalHomes.Api.Common;
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
        var items = await _db.BookingDeposits.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await _db.BookingDeposits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item is null ? NotFound(ApiResponse<object>.Fail("Deposit not found.")) : Ok(ApiResponse<object>.Ok(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewDepositRequest request, CancellationToken cancellationToken)
    {
        if (request.BookingId <= 0 || request.Amount <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Booking and amount are required."));
        }

        var deposit = new BookingDeposit
        {
            BookingId = request.BookingId,
            Amount = request.Amount,
            DeadlineAt = request.DeadlineAt,
            PaymentCardId = request.PaymentCardId,
            Note = TextRules.CleanOptional(request.Note),
            Status = BookingDepositStatus.Waiting
        };

        _db.BookingDeposits.Add(deposit);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { deposit.Id }));
    }

    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, UpdateDepositStatusInput input, CancellationToken cancellationToken)
    {
        var deposit = await _db.BookingDeposits.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (deposit is null)
        {
            return NotFound(ApiResponse<object>.Fail("Deposit not found."));
        }

        deposit.Status = input.Status;
        deposit.Note = TextRules.CleanOptional(input.Note) ?? deposit.Note;

        if (input.Status == BookingDepositStatus.Paid)
        {
            deposit.PaidAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { deposit.Id, deposit.Status }));
    }

    [HttpPost("{id:long}/reminder")]
    public async Task<IActionResult> SendReminder(long id, SendDepositReminderInput input, CancellationToken cancellationToken)
    {
        if (TextRules.Empty(input.To))
        {
            return BadRequest(ApiResponse<object>.Fail("Receiver is required."));
        }

        var deposit = await _db.BookingDeposits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (deposit is null)
        {
            return NotFound(ApiResponse<object>.Fail("Deposit not found."));
        }

        var text = input.CustomText ?? $"Beh odenisi ucun mebleg: {deposit.Amount} AZN. Zehmet olmasa vaxtinda gonderin.";
        var providerId = await _messageSender.SendAsync(MessageChannel.WhatsApp, TextRules.Clean(input.To), text, cancellationToken);

        var message = new OutboundMessage
        {
            Channel = MessageChannel.WhatsApp,
            Status = MessageStatus.Sent,
            To = TextRules.Clean(input.To),
            Text = text,
            ProviderMessageId = providerId,
            SentAt = DateTime.UtcNow,
            BookingId = deposit.BookingId,
            BookingDepositId = deposit.Id
        };

        _db.OutboundMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { message.Id }));
    }
}
