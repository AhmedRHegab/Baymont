using Baymont.Application.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Baymont.Api.Middleware;

/// <summary>Turns ASP.NET's English model-binding errors into one short Arabic sentence.</summary>
public static class ModelErrors
{
    public static string Describe(ModelStateDictionary state)
    {
        var messages = new List<string>();
        const string unreadable = "البيانات المرسلة غير صالحة. تأكد من ملء كل الحقول بشكل صحيح.";

        // A body that is missing or not valid JSON: say so once; the per-field errors that follow are noise.
        if (state.Any(e => e.Value is { Errors.Count: > 0 } && (string.IsNullOrEmpty(e.Key) || e.Key is "request" or "$")))
            return unreadable;

        foreach (var (key, entry) in state)
        {
            if (entry.Errors.Count == 0) continue;
            var property = LastSegment(key);

            var label = FieldLabels.Get(null, property);
            var missing = entry.Errors.Any(e => e.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase));
            messages.Add(missing
                ? $"الحقل «{label}» مطلوب."
                : $"القيمة المدخلة في «{label}» غير صالحة.");
        }

        var distinct = messages.Distinct().Take(3).ToList();
        return distinct.Count == 0 ? "البيانات المدخلة غير صحيحة." : string.Join(" ", distinct);
    }

    private static string LastSegment(string key)
    {
        // "$.entries[0].entryDate" -> "entryDate", "FullName" -> "FullName"
        var cut = key.LastIndexOfAny(['.', ']']);
        return cut >= 0 ? key[(cut + 1)..] : key;
    }
}
