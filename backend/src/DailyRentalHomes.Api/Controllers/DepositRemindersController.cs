using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/deposit-reminders")]
public sealed class DepositRemindersController : ControllerBase
{
    private readonly AppDbContext _db;

    public DepositRemindersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("due")]
    public async Task<IActionResult> GetDue(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var until = now.AddHours(24);

        var items = await _db.BookingDeposits
            .AsNoTracking()
            .Where(x => x.Status == BookingDepositStatus.Waiting)
            .Where(x => x.DeadlineAt != null && x.DeadlineAt <= until)
            .OrderBy(x => x.DeadlineAt)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(items));
    }
}
