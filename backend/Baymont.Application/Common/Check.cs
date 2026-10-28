namespace Baymont.Application.Common;

/// <summary>Small reusable checks. Every message is Arabic and says which field is wrong and what to do.</summary>
public static class Check
{
    public static readonly DateOnly MinDate = new(2000, 1, 1);
    public static readonly DateOnly MaxDate = new(2100, 12, 31);

    public static void Required(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ValidationException($"الحقل «{label}» مطلوب.");
    }

    public static void MaxLength(string? value, int max, string label)
    {
        if (value is not null && value.Trim().Length > max)
            throw new ValidationException($"الحقل «{label}» طويل. الحد الأقصى {max} حرفاً.");
    }

    public static void Date(DateOnly? value, string label)
    {
        if (value is { } d && (d < MinDate || d > MaxDate))
            throw new ValidationException($"التاريخ في حقل «{label}» غير منطقي. اكتب تاريخاً بين سنة 2000 وسنة 2100.");
    }

    public static void Range(int value, int min, int max, string label)
    {
        if (value < min || value > max)
            throw new ValidationException($"قيمة «{label}» يجب أن تكون بين {min} و{max}.");
    }

    public static void Defined<T>(T value, string label) where T : struct, Enum
    {
        if (!Enum.IsDefined(value)) throw new ValidationException($"القيمة المختارة في «{label}» غير صالحة.");
    }

    /// <summary>
    /// Numbers may be written with spaces, dashes, brackets, "+", and Arabic or English digits; two numbers can be
    /// separated by "/" or ",". It must contain at least 5 digits. Empty is fine (the field is optional).
    /// </summary>
    public static void Phone(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var digits = 0;
        foreach (var ch in value)
        {
            if (char.IsDigit(ch)) { digits++; continue; }
            if (ch is ' ' or '+' or '-' or '(' or ')' or '/' or ',' or '،' or '.') continue;
            throw new ValidationException($"«{label}» يجب أن يحتوي على أرقام فقط (يمكن استخدام + و - والمسافة).");
        }
        if (digits < 5 || digits > 30)
            throw new ValidationException($"«{label}» غير صحيح. اكتب الرقم كاملاً.");
    }

    public static bool IsValidPhone(string? value)
    {
        try { Phone(value, "x"); return true; }
        catch (ValidationException) { return false; }
    }

    /// <summary>The end may not be before the start.</summary>
    public static void Order(DateOnly? from, DateOnly? to, string message)
    {
        if (from is { } f && to is { } t && t < f) throw new ValidationException(message);
    }
}
