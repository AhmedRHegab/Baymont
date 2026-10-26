using Baymont.Application.Services;
using Baymont.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
[Route("api/audit-logs")]
public class AuditLogsController(AuditQueryService auditQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] string? userId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
        => Ok(await auditQueryService.GetAsync(entityType, entityId, userId, from, to, take, ct));
}
