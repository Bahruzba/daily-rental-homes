using DailyRentalHomes.Api.Contracts.Deposits;
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

    public DepositsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.BookingDeposits.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(items);
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
}
