using Baymont.Application.Interfaces;
using Baymont.Application.Services;
using Baymont.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
[Route("api/inbox")]
public class InboxController(IAppDbContext db, InboxStatus status) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var messages = await db.InboxMessages.AsNoTracking()
            .OrderByDescending(m => m.ProcessedAt)
            .Take(50)
            .Select(m => new
            {
                m.Id, m.FromAddress, m.Subject, m.ReceivedAt, m.ProcessedAt, m.Status, m.Detail, m.PermitsCreated
            })
            .ToListAsync(ct);

        return Ok(new
        {
            status.Enabled,
            status.Mailbox,
            status.PollSeconds,
            status.LastPollAt,
            status.LastError,
            status.Polling,
            messages
        });
    }

    /// <summary>Ask the reader to look at the mailbox now instead of waiting for the next round.</summary>
    [HttpPost("check")]
    public IActionResult Check()
    {
        if (!status.Enabled) return BadRequest(new { message = "قارئ البريد غير مفعّل." });
        status.RequestCheck();
        return NoContent();
    }
}
