using Baymont.Application.Services;
using Baymont.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/demo")]
public class DemoController(DemoDataService demo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken ct) => Ok(await demo.StatusAsync(ct));

    /// <summary>Permanently deletes all sample data.</summary>
    [HttpPost("clear")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Clear(CancellationToken ct) => Ok(new { deleted = await demo.ClearAsync(ct) });
}
