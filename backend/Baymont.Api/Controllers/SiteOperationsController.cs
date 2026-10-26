using Baymont.Application.Dtos;
using Baymont.Application.Services;
using Baymont.Domain.Constants;
using Baymont.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class SiteOperationsController(SiteOperationsService svc) : ControllerBase
{
    private const string Writers = $"{AppRoles.Admin},{AppRoles.Editor}";

    // Overnight owners
    [HttpGet("overnight-stays")]
    public async Task<IActionResult> Overnight([FromQuery] DateOnly date, [FromQuery] ShiftPeriod shift, CancellationToken ct)
        => Ok(await svc.OvernightAsync(date, shift, ct));

    [HttpPost("overnight-stays")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> AddOvernight([FromBody] SaveOvernightStayRequest r, CancellationToken ct)
        => Ok(new { id = await svc.AddOvernightAsync(r, ct) });

    [HttpDelete("overnight-stays/{id:guid}")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> DeleteOvernight(Guid id, CancellationToken ct)
    {
        await svc.DeleteOvernightAsync(id, ct);
        return NoContent();
    }

    // Staff
    [HttpGet("staff")]
    public async Task<IActionResult> Staff([FromQuery] string? q, CancellationToken ct)
        => Ok(await svc.StaffAsync(q, ct));

    [HttpPost("staff")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> CreateStaff([FromBody] SaveStaffRequest r, CancellationToken ct)
        => Ok(new { id = await svc.CreateStaffAsync(r, ct) });

    [HttpPut("staff/{id:guid}")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> UpdateStaff(Guid id, [FromBody] SaveStaffRequest r, CancellationToken ct)
    {
        await svc.UpdateStaffAsync(id, r, ct);
        return NoContent();
    }

    [HttpDelete("staff/{id:guid}")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> DeleteStaff(Guid id, CancellationToken ct)
    {
        await svc.DeleteStaffAsync(id, ct);
        return NoContent();
    }

    [HttpPost("staff/{id:guid}/enter")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> Enter(Guid id, CancellationToken ct)
    {
        await svc.EnterAsync(id, ct);
        return NoContent();
    }

    [HttpPost("staff/{id:guid}/exit")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> Exit(Guid id, CancellationToken ct)
    {
        await svc.ExitAsync(id, ct);
        return NoContent();
    }

    [HttpGet("staff/log")]
    public async Task<IActionResult> StaffLog([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken ct)
        => Ok(await svc.StaffLogAsync(from, to, ct));

    [HttpGet("on-site")]
    public async Task<IActionResult> OnSite(CancellationToken ct) => Ok(await svc.OnSiteAsync(ct));

    // Shift roster
    [HttpGet("shifts")]
    public async Task<IActionResult> Shifts([FromQuery] DateOnly date, CancellationToken ct)
        => Ok(await svc.ShiftsAsync(date, ct));

    [HttpPost("shifts")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> CreateShift([FromBody] SaveShiftAssignmentRequest r, CancellationToken ct)
        => Ok(new { id = await svc.CreateShiftAsync(r, ct) });

    [HttpPut("shifts/{id:guid}")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> UpdateShift(Guid id, [FromBody] SaveShiftAssignmentRequest r, CancellationToken ct)
    {
        await svc.UpdateShiftAsync(id, r, ct);
        return NoContent();
    }

    [HttpDelete("shifts/{id:guid}")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> DeleteShift(Guid id, CancellationToken ct)
    {
        await svc.DeleteShiftAsync(id, ct);
        return NoContent();
    }
}
