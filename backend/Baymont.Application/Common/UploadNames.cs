using System.Text.RegularExpressions;

namespace Baymont.Application.Common;

public static partial class UploadNames
{
    public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".pdf"];

    [GeneratedRegex(@"^[0-9a-f]{32}\.(jpg|jpeg|png|webp|pdf)$")]
    private static partial Regex StoredName();

    public static bool IsValid(string name) => StoredName().IsMatch(name);

    /// <summary>Returns the stored file name, or null when empty. Rejects anything that is not one of our generated names.</summary>
    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var name = value.Trim();
        if (!IsValid(name)) throw new ValidationException("مسار الملف غير صالح.");
        return name;
    }
}
