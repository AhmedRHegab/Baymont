using Baymont.Application.Dtos;
using Baymont.Application.Services;
using Baymont.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/units")]
public class UnitsController(UnitService unitService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Lookup([FromQuery] string? q, CancellationToken ct)
        => Ok(await unitService.LookupAsync(q, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await unitService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest request, CancellationToken ct)
        => Ok(await unitService.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitRequest request, CancellationToken ct)
        => Ok(await unitService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await unitService.DeleteAsync(id, ct);
        return NoContent();
    }
}
