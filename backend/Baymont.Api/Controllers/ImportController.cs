using Baymont.Application.Services;
using Baymont.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/import")]
public class ImportController(ImportService importService) : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024;

    [HttpGet("template")]
    public IActionResult Template()
        => File(importService.BuildTemplate(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "نموذج-استيراد-الملاك.xlsx");

    /// <summary>Checks the file and reports what would be added. Saves nothing.</summary>
    [HttpPost("preview")]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)]
    public Task<IActionResult> Preview(IFormFile? file, CancellationToken ct) => Run(file, commit: false, ct);

    /// <summary>Saves the file. Nothing is saved if any row has an error.</summary>
    [HttpPost("commit")]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)]
    public Task<IActionResult> Commit(IFormFile? file, CancellationToken ct) => Run(file, commit: true, ct);

    private async Task<IActionResult> Run(IFormFile? file, bool commit, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "لم يتم اختيار ملف." });
        if (file.Length > MaxBytes)
            return BadRequest(new { message = "حجم الملف أكبر من 5 ميجابايت." });
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "الملف يجب أن يكون بصيغة xlsx. افتحه في Excel واحفظه باسم جديد بهذه الصيغة." });

        await using var stream = file.OpenReadStream();
        return Ok(await importService.RunAsync(stream, commit, ct));
    }
}
