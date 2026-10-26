using Baymont.Application.Dtos;
using Baymont.Domain.Constants;
using Baymont.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/owners")]
public class OwnersController(OwnerService ownerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Lookup([FromQuery] string? q, CancellationToken ct)
        => Ok(await ownerService.LookupAsync(q, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await ownerService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
    public async Task<IActionResult> Create([FromBody] CreateOwnerRequest request, CancellationToken ct)
        => Ok(await ownerService.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOwnerRequest request, CancellationToken ct)
        => Ok(await ownerService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await ownerService.DeleteAsync(id, ct);
        return NoContent();
    }
}
