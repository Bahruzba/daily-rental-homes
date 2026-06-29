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
        var items = await _db.PaymentCards.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewPaymentCardRequest request, CancellationToken cancellationToken)
    {
        var card = new PaymentCard
        {
            BrokerUserId = request.BrokerUserId,
            CardHolderName = request.CardHolderName,
            PanMasked = request.PanMasked
        };

        _db.PaymentCards.Add(card);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(card.Id);
    }
}
