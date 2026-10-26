using Baymont.Application.Dtos;
using Baymont.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/import")]
public class ImportController : ControllerBase
{
    [HttpPost("preview")]
    [RequestSizeLimit(20_000_000)]
    public ActionResult<ImportStubResponse> Preview(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "يرجى رفع ملف Excel." });

        return Ok(new ImportStubResponse(
            "استيراد الإكسيل غير مفعّل بعد. سيتم ربط الأعمدة بعد استلام الملف الفعلي (مصطبة 1/2، التسليمات، الحجوزات).",
            Implemented: false));
    }

    [HttpPost("commit")]
    public ActionResult<ImportStubResponse> Commit([FromBody] object? _)
        => Ok(new ImportStubResponse(
            "تنفيذ الاستيراد مؤجل حتى اكتمال خريطة الأعمدة من ملف الإكسيل.",
            Implemented: false));
}
