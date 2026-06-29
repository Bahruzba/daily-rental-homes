using Microsoft.AspNetCore.Mvc;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/rental-homes")]
public sealed class RentalHomesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetList()
    {
        return Ok("rental homes endpoint");
    }
}
