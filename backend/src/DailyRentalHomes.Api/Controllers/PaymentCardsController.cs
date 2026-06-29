using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.PaymentCards;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/payment-cards")]
public sealed class PaymentCardsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PaymentCardsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.PaymentCards.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewPaymentCardRequest request, CancellationToken cancellationToken)
    {
        if (request.BrokerUserId <= 0 || TextRules.Empty(request.CardHolderName) || TextRules.Empty(request.PanMasked))
        {
            return BadRequest(ApiResponse<object>.Fail("Broker, card holder and PAN are required."));
        }

        var card = new PaymentCard
        {
            BrokerUserId = request.BrokerUserId,
            CardHolderName = TextRules.Clean(request.CardHolderName),
            PanMasked = TextRules.Clean(request.PanMasked)
        };

        _db.PaymentCards.Add(card);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { card.Id }));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, NewPaymentCardRequest request, CancellationToken cancellationToken)
    {
        var card = await _db.PaymentCards.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (card is null)
        {
            return NotFound(ApiResponse<object>.Fail("Payment card not found."));
        }

        card.BrokerUserId = request.BrokerUserId;
        card.CardHolderName = TextRules.Clean(request.CardHolderName);
        card.PanMasked = TextRules.Clean(request.PanMasked);
        card.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { card.Id }));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var card = await _db.PaymentCards.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (card is null)
        {
            return NotFound(ApiResponse<object>.Fail("Payment card not found."));
        }

        card.IsDeleted = true;
        card.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { card.Id }));
    }
}
