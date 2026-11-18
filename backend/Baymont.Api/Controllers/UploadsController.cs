using Baymont.Application.Common;
using Baymont.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baymont.Api.Controllers;

/// <summary>Photos/PDFs (ID cards, criminal records, equipment). Stored on disk, never served statically.</summary>
[ApiController]
[Route("api/uploads")]
public class UploadsController(IWebHostEnvironment env) : ControllerBase
{
    private const long MaxBytes = 8 * 1024 * 1024;
    private const string Reviewers = $"{AppRoles.Admin},{AppRoles.Editor}";
    // The gate guards photograph the ID cards of the people entering.
    private const string Gate = $"{AppRoles.Admin},{AppRoles.Editor},{AppRoles.SearchOnly}";

    private string Folder => Path.Combine(env.ContentRootPath, "App_Data", "uploads");

    [HttpPost]
    [Authorize(Roles = Reviewers)]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)]
    public Task<IActionResult> Upload(IFormFile? file, CancellationToken ct) => Save(file, imagesOnly: false, ct);

    /// <summary>A photo of an ID card. Anyone at the gate may add one; only admins and editors can open it afterwards.</summary>
    [HttpPost("id")]
    [Authorize(Roles = Gate)]
    [RequestSizeLimit(MaxBytes + 1024 * 1024)]
    public Task<IActionResult> UploadId(IFormFile? file, CancellationToken ct) => Save(file, imagesOnly: true, ct);

    private async Task<IActionResult> Save(IFormFile? file, bool imagesOnly, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "لم يتم اختيار ملف." });
        if (file.Length > MaxBytes)
            return BadRequest(new { message = "حجم الملف أكبر من 8 ميجابايت." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!UploadNames.AllowedExtensions.Contains(ext) || (imagesOnly && ext == ".pdf"))
            return BadRequest(new { message = imagesOnly ? "النوع غير مدعوم. المسموح: صورة." : "النوع غير مدعوم. المسموح: صور أو PDF." });

        var header = new byte[12];
        await using (var peek = file.OpenReadStream())
        {
            var read = await peek.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
            if (!MatchesExtension(ext, header.AsSpan(0, read)))
                return BadRequest(new { message = "محتوى الملف لا يطابق نوعه." });
        }

        Directory.CreateDirectory(Folder);
        var name = $"{Guid.NewGuid():N}{ext}";
        await using (var target = System.IO.File.Create(Path.Combine(Folder, name)))
        {
            await file.CopyToAsync(target, ct);
        }

        return Ok(new { path = name });
    }

    [HttpGet("{name}")]
    [Authorize(Roles = Reviewers)]
    public IActionResult Get(string name)
    {
        // Only our generated names are accepted, so no path traversal is possible.
        if (!UploadNames.IsValid(name)) return NotFound();
        var full = Path.Combine(Folder, name);
        if (!System.IO.File.Exists(full)) return NotFound();

        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "private, max-age=3600";
        return PhysicalFile(full, ContentTypeFor(Path.GetExtension(name)));
    }

    private static string ContentTypeFor(string ext) => ext switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        _ => "image/jpeg"
    };

    private static bool MatchesExtension(string ext, ReadOnlySpan<byte> h) => ext switch
    {
        ".jpg" or ".jpeg" => h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF,
        ".png" => h.Length >= 4 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47,
        ".pdf" => h.Length >= 4 && h[0] == 0x25 && h[1] == 0x50 && h[2] == 0x44 && h[3] == 0x46,
        ".webp" => h.Length >= 12 && h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F'
                   && h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P',
        _ => false
    };
}
