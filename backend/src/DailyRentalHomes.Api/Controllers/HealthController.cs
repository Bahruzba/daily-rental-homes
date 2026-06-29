using Microsoft.AspNetCore.Mvc;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("ok");
    }
}
