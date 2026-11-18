using Baymont.Domain.Enums;

namespace Baymont.Application.Common;

public record NationalIdInfo(string Number, DateOnly BirthDate, PersonGender Gender, string GovernorateCode, string Governorate);

/// <summary>
/// The Egyptian national number has 14 digits: century (2 = 1900s, 3 = 2000s), birth date (YYMMDD),
/// governorate (2 digits), a 4-digit serial whose 3rd digit is odd for men and even for women, and a check digit.
/// The check digit has no published rule, so only the structure is checked: a misread digit that still gives a
/// believable date and governorate cannot be caught, which is why a person always confirms the number.
/// </summary>
public static class EgyptianNationalId
{
    private static readonly Dictionary<string, string> Governorates = new()
    {
        ["01"] = "القاهرة", ["02"] = "الإسكندرية", ["03"] = "بورسعيد", ["04"] = "السويس",
        ["11"] = "دمياط", ["12"] = "الدقهلية", ["13"] = "الشرقية", ["14"] = "القليوبية",
        ["15"] = "كفر الشيخ", ["16"] = "الغربية", ["17"] = "المنوفية", ["18"] = "البحيرة",
        ["19"] = "الإسماعيلية", ["21"] = "الجيزة", ["22"] = "بني سويف", ["23"] = "الفيوم",
        ["24"] = "المنيا", ["25"] = "أسيوط", ["26"] = "سوهاج", ["27"] = "قنا",
        ["28"] = "أسوان", ["29"] = "الأقصر", ["31"] = "البحر الأحمر", ["32"] = "الوادي الجديد",
        ["33"] = "مطروح", ["34"] = "شمال سيناء", ["35"] = "جنوب سيناء", ["88"] = "خارج الجمهورية"
    };

    public static string GovernorateName(string? code)
        => code is not null && Governorates.TryGetValue(code, out var name) ? name : string.Empty;

    /// <summary>Keeps the digits only, turning Arabic-Indic (٠-٩) and Persian (۰-۹) digits into 0-9.</summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            if (ch is >= '0' and <= '9') sb.Append(ch);
            else if (ch is >= '٠' and <= '٩') sb.Append((char)('0' + (ch - '٠')));
            else if (ch is >= '۰' and <= '۹') sb.Append((char)('0' + (ch - '۰')));
        }
        return sb.ToString();
    }

    public static bool TryParse(string? raw, out NationalIdInfo? info, out string error)
    {
        info = null;
        var n = Normalize(raw);

        if (n.Length != 14)
        {
            error = "الرقم القومي يجب أن يتكون من 14 رقماً.";
            return false;
        }

        var century = n[0] switch { '2' => 1900, '3' => 2000, _ => 0 };
        if (century == 0)
        {
            error = "الرقم القومي غير صحيح: أول رقم يجب أن يكون 2 أو 3.";
            return false;
        }

        var year = century + int.Parse(n.AsSpan(1, 2));
        var month = int.Parse(n.AsSpan(3, 2));
        var day = int.Parse(n.AsSpan(5, 2));
        if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            error = "الرقم القومي غير صحيح: تاريخ الميلاد بداخله غير منطقي.";
            return false;
        }

        var birth = new DateOnly(year, month, day);
        if (birth > EgyptClock.Today)
        {
            error = "الرقم القومي غير صحيح: تاريخ الميلاد بداخله في المستقبل.";
            return false;
        }

        var code = n.Substring(7, 2);
        if (!Governorates.TryGetValue(code, out var governorate))
        {
            error = "الرقم القومي غير صحيح: كود المحافظة غير معروف.";
            return false;
        }

        var gender = (n[12] - '0') % 2 == 1 ? PersonGender.Male : PersonGender.Female;
        info = new NationalIdInfo(n, birth, gender, code, governorate);
        error = string.Empty;
        return true;
    }

    /// <summary>Empty input means "no ID given" and is fine; anything else must be a valid number.</summary>
    public static NationalIdInfo? ParseOrNull(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!TryParse(raw, out var info, out var error)) throw new ValidationException(error);
        return info;
    }

    /// <summary>"**********5678": enough to tell two people apart, not enough to misuse.</summary>
    public static string Mask(string? number)
        => string.IsNullOrEmpty(number) ? string.Empty : new string('*', Math.Max(0, number.Length - 4)) + number[^4..];
}
