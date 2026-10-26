using Baymont.Application.Dtos;
using Baymont.Application.Services;
using Baymont.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/contractors")]
public class ContractorsController(ContractorService contractorService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, CancellationToken ct)
        => Ok(await contractorService.ListAsync(q, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await contractorService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
    public async Task<IActionResult> Create([FromBody] SaveContractorRequest request, CancellationToken ct)
        => Ok(await contractorService.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveContractorRequest request, CancellationToken ct)
        => Ok(await contractorService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await contractorService.DeleteAsync(id, ct);
        return NoContent();
    }
}
