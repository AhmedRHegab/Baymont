using Baymont.Application.Dtos;
using Baymont.Application.Services;
using Baymont.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/deliveries")]
public class DeliveriesController(DeliveryService deliveryService) : ControllerBase
{
    private const string Writers = $"{AppRoles.Admin},{AppRoles.Editor},{AppRoles.Overhead}";

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] bool? received, CancellationToken ct)
        => Ok(await deliveryService.ListAsync(q, from, to, received, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await deliveryService.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> Create([FromBody] SaveDeliveryRequest request, CancellationToken ct)
        => Ok(await deliveryService.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Writers)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveDeliveryRequest request, CancellationToken ct)
        => Ok(await deliveryService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Editor}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await deliveryService.DeleteAsync(id, ct);
        return NoContent();
    }
}
