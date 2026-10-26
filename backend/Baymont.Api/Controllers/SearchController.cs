using Baymont.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/search")]
public class SearchController(SearchService searchService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken ct)
        => Ok(await searchService.SearchAsync(q, ct));
}
