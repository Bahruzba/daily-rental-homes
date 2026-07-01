using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
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

        var query = _db.BookingDeposits.AsNoTracking();
        if (!User.IsAdmin())
        {
            var userId = User.GetUserId();
            query = query.Where(x => x.Booking!.RentalHome!.BrokerUserId == userId);
        }

        var items = await query
            .Where(x => x.Status == BookingDepositStatus.Waiting)
            .Where(x => x.DeadlineAt != null && x.DeadlineAt <= until)
            .OrderBy(x => x.DeadlineAt)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(items));
    }
}
