using Baymont.Application.Common;
using Baymont.Application.Dtos;
using Baymont.Application.Services;
using Baymont.Domain.Constants;
using Baymont.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reservations")]
public class ReservationsController(ReservationService svc) : ControllerBase
{
    private const string Reviewers = $"{AppRoles.Admin},{AppRoles.Editor}";
    // Permits arrive by email. Adding one by hand (or uploading a file) is an emergency fallback for admins only.
    private const string Fallback = AppRoles.Admin;
    // Gate guards (SearchOnly) record arrivals and departures.
    private const string Gate = $"{AppRoles.Admin},{AppRoles.Editor},{AppRoles.SearchOnly}";
    private const long MaxBytes = 5 * 1024 * 1024;

    private bool IsReviewer => User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Editor);

    [HttpGet]
    [Authorize(Roles = Gate)]
    public async Task<IActionResult> List([FromQuery] ReservationStatus? status, [FromQuery] string? q, CancellationToken ct)
        // Unreviewed permits are not shown to the gate.
        => Ok(await svc.ListAsync(IsReviewer ? status ?? ReservationStatus.Approved : ReservationStatus.Approved, q, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Gate)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var r = await svc.GetAsync(id, ct);
        if (!IsReviewer && r.Status != ReservationStatus.Approved) return NotFound();
        return Ok(r);
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = Gate)]
    public async Task<IActionResult> Dashboard([FromQuery] DateOnly? today, [FromQuery] DateOnly? start, CancellationToken ct)
        // The browser sends its own local date; otherwise use the date at the site (Egypt).
        => Ok(await svc.DashboardAsync(today ?? EgyptClock.Today, start, ct));

    [HttpGet("pending-count")]
    [Authorize(Roles = Reviewers)]
    public async Task<IActionResult> PendingCount(CancellationToken ct) => Ok(new { count = await svc.PendingCountAsync(ct) });

    [HttpPost]
    [Authorize(Roles = Fallback)]
    public async Task<IActionResult> Create([FromBody] SaveReservationRequest r, CancellationToken ct)
        => Ok(await svc.CreateAsync(r, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Reviewers)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveReservationRequest r, CancellationToken ct)
        => Ok(await svc.UpdateAsync(id, r, ct));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Reviewers)]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        await svc.RejectAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Reviewers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await svc.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/enter")]
    [Authorize(Roles = Gate)]
    public async Task<IActionResult> Enter(Guid id, CancellationToken ct)
    {
        await svc.EnterAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/exit")]
    [Authorize(Roles = Gate)]
    public async Task<IActionResult> Exit(Guid id, CancellationToken ct)
    {
        await svc.ExitAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/final-leave")]
    [Authorize(Roles = Gate)]
    public async Task<IActionResult> FinalLeave(Guid id, CancellationToken ct)
    {
        await svc.FinalLeaveAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/undo")]
    [Authorize(Roles = Gate)]
    public async Task<IActionResult> Undo(Guid id, CancellationToken ct)
    {
        await svc.UndoAsync(id, ct);
        return NoContent();
    }

    /// <summary>Emergency fallback: upload a permit sheet by hand. New permits go to the review queue.</summary>
    [HttpPost("import")]
    [Authorize(Roles = Fallback)]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "لم يتم اختيار ملف." });
        if (file.Length > MaxBytes) return BadRequest(new { message = "حجم الملف أكبر من 5 ميجابايت." });
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "الملف يجب أن يكون بصيغة xlsx." });

        await using var stream = file.OpenReadStream();
        return Ok(await svc.ImportAsync(stream, new ReservationSource("Upload", User.Identity?.Name, null, file.FileName, null, DateTimeOffset.UtcNow), ct));
    }
}
