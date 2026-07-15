using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Deposits;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/admin/deposit-deadline-reminders")]
public sealed class AdminDepositDeadlineRemindersController : ControllerBase
{
    private readonly IDepositDeadlineReminderProcessingService _processor;

    public AdminDepositDeadlineRemindersController(IDepositDeadlineReminderProcessingService processor)
    {
        _processor = processor;
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process(CancellationToken cancellationToken)
    {
        var result = await _processor.ProcessAsync(cancellationToken);
        var response = new DepositDeadlineReminderProcessingResponse(
            result.Evaluated,
            result.Eligible,
            result.Queued,
            result.DuplicateSkipped);
        return Ok(ApiResponse<DepositDeadlineReminderProcessingResponse>.Ok(response));
    }
}
