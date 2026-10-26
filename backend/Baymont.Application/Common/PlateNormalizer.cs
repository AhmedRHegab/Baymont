using System.Text;

namespace Baymont.Application.Common;

public static class PlateNormalizer
{
    public static string Normalize(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return string.Empty;

        var sb = new StringBuilder(plate.Length);
        foreach (var ch in plate)
        {
            if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '.')
                continue;

            sb.Append(ch switch
            {
                '٠' => '0',
                '١' => '1',
                '٢' => '2',
                '٣' => '3',
                '٤' => '4',
                '٥' => '5',
                '٦' => '6',
                '٧' => '7',
                '٨' => '8',
                '٩' => '9',
                '۰' => '0',
                '۱' => '1',
                '۲' => '2',
                '۳' => '3',
                '۴' => '4',
                '۵' => '5',
                '۶' => '6',
                '۷' => '7',
                '۸' => '8',
                '۹' => '9',
                _ => char.ToUpperInvariant(ch)
            });
        }

        return sb.ToString();
    }
}
