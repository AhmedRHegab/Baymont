using System.Globalization;
using Baymont.Application.Dtos;
using Baymont.Domain.Enums;
using ClosedXML.Excel;

namespace Baymont.Application.Services;

public record ParsedReservation(
    int Row, string UnitCode, string GuestNames, int PersonsCount,
    DateOnly? CheckIn, DateOnly? CheckOut, VisitorType? Type);

public record ParsedReservations(IReadOnlyList<ParsedReservation> Rows, IReadOnlyList<ImportRowIssueDto> Issues);

/// <summary>
/// Reads the permit sheet. Columns are found by header name (any order, hidden columns included):
/// "Deal Name" (unit code) and "Full Name" are required; "Check In", "Check Out" and "Type" are optional.
/// Rows hidden by the sender's filter are skipped, and every sheet that has the headers is read.
/// </summary>
public static class ReservationExcelParser
{
    private static readonly string[] UnitHeaders = ["dealname", "unitcode", "unit", "رقمالوحدة", "كودالوحدة", "الوحدة"];
    private static readonly string[] NameHeaders = ["fullname", "name", "guestname", "guestnames", "guests", "الاسم", "اسمالضيف", "الاسماء", "الأسماء"];
    private static readonly string[] CheckInHeaders = ["checkin", "arrival", "تاريخالوصول", "تاريخالدخول", "منتاريخ"];
    private static readonly string[] CheckOutHeaders = ["checkout", "departure", "تاريخالمغادرة", "تاريخالخروج", "حتىتاريخ"];
    private static readonly string[] TypeHeaders = ["type", "visitortype", "visittype", "نوعالزيارة", "النوع"];

    private static readonly string[] DateFormats =
    [
        "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy",
        "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss"
    ];

    public static ParsedReservations Parse(Stream file)
    {
        var rows = new List<ParsedReservation>();
        var issues = new List<ImportRowIssueDto>();

        try
        {
            using var wb = new XLWorkbook(file);
            var foundSheet = false;

            foreach (var ws in wb.Worksheets)
            {
                var cols = FindHeader(ws, out var headerRow);
                if (cols is null) continue;
                foundSheet = true;
                ReadSheet(ws, headerRow, cols, rows, issues);
            }

            if (!foundSheet)
                issues.Add(new ImportRowIssueDto(0,
                    "لم يتم العثور على عمودي «Deal Name» و«Full Name» في أي ورقة داخل الملف."));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            issues.Add(new ImportRowIssueDto(0, "تعذر قراءة الملف. تأكد أنه ملف Excel بصيغة xlsx وغير محمي بكلمة سر."));
        }

        return new ParsedReservations(rows, issues);
    }

    private record Columns(int Unit, int Name, int? CheckIn, int? CheckOut, int? Type);

    private static Columns? FindHeader(IXLWorksheet ws, out int headerRow)
    {
        headerRow = 0;
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 0, 10);

        for (var r = 1; r <= lastRow; r++)
        {
            int? unit = null, name = null, ci = null, co = null, ty = null;
            for (var c = 1; c <= lastCol; c++)
            {
                var h = Norm(ws.Cell(r, c).GetString());
                if (h.Length == 0) continue;
                if (unit is null && UnitHeaders.Contains(h)) unit = c;
                else if (name is null && NameHeaders.Contains(h)) name = c;
                else if (ci is null && CheckInHeaders.Contains(h)) ci = c;
                else if (co is null && CheckOutHeaders.Contains(h)) co = c;
                else if (ty is null && TypeHeaders.Contains(h)) ty = c;
            }

            if (unit is not null && name is not null)
            {
                headerRow = r;
                return new Columns(unit.Value, name.Value, ci, co, ty);
            }
        }
        return null;
    }

    private static void ReadSheet(IXLWorksheet ws, int headerRow, Columns cols,
        List<ParsedReservation> rows, List<ImportRowIssueDto> issues)
    {
        var last = ws.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var r = headerRow + 1; r <= last; r++)
        {
            if (ws.Row(r).IsHidden) continue;

            var unit = ws.Cell(r, cols.Unit).GetString().Trim();
            var names = ws.Cell(r, cols.Name).GetString().Trim();
            if (unit.Length == 0 && names.Length == 0) continue;

            var label = $"{ws.Name} / سطر {r}";
            if (unit.Length == 0) { issues.Add(new ImportRowIssueDto(r, $"{label}: كود الوحدة فارغ، تم تجاهل السطر.")); continue; }
            if (names.Length == 0) { issues.Add(new ImportRowIssueDto(r, $"{label}: الاسم فارغ، تم تجاهل السطر.")); continue; }

            if (unit.Length > 128) { issues.Add(new ImportRowIssueDto(r, $"{label}: كود الوحدة طويل جداً، تم تجاهل السطر.")); continue; }
            if (names.Length > 1024) { issues.Add(new ImportRowIssueDto(r, $"{label}: الأسماء طويلة جداً، تم تجاهل السطر.")); continue; }
            if (CountPersons(names) > 100) { issues.Add(new ImportRowIssueDto(r, $"{label}: عدد الأسماء أكبر من 100، تم تجاهل السطر.")); continue; }

            var checkIn = cols.CheckIn is null ? null : ParseDate(ws.Cell(r, cols.CheckIn.Value), out var badIn);
            var checkOut = cols.CheckOut is null ? null : ParseDate(ws.Cell(r, cols.CheckOut.Value), out var badOut);
            if (cols.CheckIn is not null && checkIn is null && !ws.Cell(r, cols.CheckIn.Value).IsEmpty())
                issues.Add(new ImportRowIssueDto(r, $"{label}: تاريخ الوصول غير مفهوم، أكمله أثناء المراجعة."));
            if (cols.CheckOut is not null && checkOut is null && !ws.Cell(r, cols.CheckOut.Value).IsEmpty())
                issues.Add(new ImportRowIssueDto(r, $"{label}: تاريخ المغادرة غير مفهوم، أكمله أثناء المراجعة."));

            var type = cols.Type is null ? null : ParseType(ws.Cell(r, cols.Type.Value).GetString());

            rows.Add(new ParsedReservation(r, unit, names, CountPersons(names), checkIn, checkOut, type));
        }
    }

    /// <summary>Guests are joined with "/" (or similar). A hyphen can be part of a name, so it is not a separator.</summary>
    public static int CountPersons(string names)
        => Math.Max(1, names.Split(['/', '\\', '،', ';', '&', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length);

    private static DateOnly? ParseDate(IXLCell cell, out bool bad)
    {
        bad = false;
        if (cell.IsEmpty()) return null;

        if (cell.DataType == XLDataType.DateTime)
            return DateOnly.FromDateTime(cell.GetDateTime());

        if (cell.DataType == XLDataType.Number)
        {
            try { return DateOnly.FromDateTime(DateTime.FromOADate(cell.GetDouble())); }
            catch { bad = true; return null; }
        }

        var s = cell.GetString().Trim();
        if (DateTime.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return DateOnly.FromDateTime(d);

        bad = true;
        return null;
    }

    public static VisitorType? ParseType(string raw)
    {
        var s = raw.Trim().ToLowerInvariant();
        if (s.Length == 0) return null;
        if (s.Contains("استضاف") || s.Contains("host") || s.Contains("complimentary")) return VisitorType.Hosting;
        if (s.Contains("تأجير") || s.Contains("تاجير") || s.Contains("ايجار") || s.Contains("إيجار") || s.Contains("rent")) return VisitorType.Rental;
        if (s.Contains("سيلز") || s.Contains("sales") || s.Contains("مبيعات")) return VisitorType.Sales;
        if (s.Contains("مفوض") || s.Contains("authoriz")) return VisitorType.Authorized;
        return null;
    }

    private static string Norm(string s)
        => new string(s.Where(ch => !char.IsWhiteSpace(ch) && ch is not ('_' or '.' or '-' or ':')).ToArray()).ToLowerInvariant();
}
