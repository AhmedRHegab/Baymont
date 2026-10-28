using Baymont.Application.Services;
using Baymont.Domain.Constants;
using Baymont.Infrastructure.Persistence;
using Baymont.Infrastructure.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/demo")]
public class DemoController(DemoDataService demo, AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken ct) => Ok(await demo.StatusAsync(ct));

    /// <summary>
    /// Adds the sample data to the current database, even if it already holds records.
    /// Everything it adds is tracked, so "clear" removes exactly that and nothing else.
    /// </summary>
    [HttpPost("load")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Load(CancellationToken ct)
    {
        if ((await demo.StatusAsync(ct)).HasDemoData)
            return BadRequest(new { message = "البيانات التجريبية موجودة بالفعل." });

        try
        {
            return Ok(new { created = await DemoDataSeeder.CreateAsync(db, ct) });
        }
        catch (DbUpdateException)
        {
            // For example a plate number that already exists among the real vehicles.
            return BadRequest(new { message = "تعذر تحميل البيانات التجريبية لوجود بيانات حقيقية متعارضة معها." });
        }
    }

    /// <summary>Permanently deletes all sample data.</summary>
    [HttpPost("clear")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Clear(CancellationToken ct) => Ok(new { deleted = await demo.ClearAsync(ct) });
}
