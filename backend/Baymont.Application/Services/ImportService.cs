using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Baymont.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

/// <summary>
/// Imports owners and their units from a simple Excel sheet.
/// Columns (row 1 is the header): owner name, phone, terrace, building, floor, apartment, construction number.
/// </summary>
public class ImportService(IAppDbContext db)
{
    public const int MaxRows = 2000;

    public static readonly string[] Headers =
        ["اسم المالك", "رقم الموبايل", "المصطبة", "المبنى", "الدور", "الشقة", "الرقم الإنشائي"];

    public byte[] BuildTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("الملاك");
        ws.RightToLeft = true;
        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Column(i + 1).Width = 18;
        }

        string[] example = ["أحمد محمد", "01012345678", "1", "4", "1", "1", ""];
        for (var i = 0; i < example.Length; i++) ws.Cell(2, i + 1).SetValue(example[i]);
        ws.Cell(2, 1).Style.Font.Italic = true;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>Validates the file; when <paramref name="commit"/> is true it also saves it. Nothing is saved if any row is wrong.</summary>
    public async Task<ImportResultDto> RunAsync(Stream file, bool commit, CancellationToken ct = default)
    {
        var rows = ReadRows(file, out var fileProblem);
        if (fileProblem is not null)
            return new ImportResultDto(0, 0, 0, 0, [new ImportRowIssueDto(0, fileProblem)], false);

        var errors = new List<ImportRowIssueDto>();

        // Owners are matched by name, units by construction number or by terrace/building/floor/apartment.
        var owners = await db.Owners.ToListAsync(ct);
        var ownerByName = owners
            .GroupBy(o => Key(o.FullName))
            .ToDictionary(g => g.Key, g => g.First());
        var units = await db.Units.AsNoTracking()
            .Select(u => new { u.ConstructionNumber, u.Terrace, u.Building, u.Floor, u.Apartment })
            .ToListAsync(ct);
        var unitKeys = units.Select(u => UnitKey(u.ConstructionNumber, u.Terrace, u.Building, u.Floor, u.Apartment))
            .Where(k => k != "").ToHashSet();

        var newOwners = 0;
        var newUnits = 0;
        var skipped = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.Name))
            {
                errors.Add(new ImportRowIssueDto(r.Number, "اسم المالك فارغ. اكتب الاسم في العمود الأول."));
                continue;
            }

            var name = r.Name.Trim();
            if (name.Length > 256)
            {
                errors.Add(new ImportRowIssueDto(r.Number, "الاسم طويل جداً."));
                continue;
            }

            var hasUnit = r.Terrace != "" || r.Building != "" || r.Floor != "" || r.Apartment != "" || r.Construction != "";

            if (!ownerByName.TryGetValue(Key(name), out var owner))
            {
                owner = new Owner
                {
                    Id = Guid.NewGuid(),
                    InternalCode = $"IMP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    FullName = name,
                    Phone = Trim(r.Phone, 64),
                    CreatedAt = now,
                    UpdatedAt = now
                };
                ownerByName[Key(name)] = owner;
                newOwners++;
                if (commit) db.Owners.Add(owner);
            }

            if (!hasUnit) continue;

            var key = UnitKey(r.Construction, r.Terrace, r.Building, r.Floor, r.Apartment);
            if (!unitKeys.Add(key))
            {
                skipped++;
                continue;
            }

            newUnits++;
            if (commit)
            {
                db.Units.Add(new Unit
                {
                    Id = Guid.NewGuid(),
                    ConstructionNumber = Trim(r.Construction, 64),
                    Terrace = Trim(r.Terrace, 64),
                    Building = Trim(r.Building, 64),
                    Floor = Trim(r.Floor, 32),
                    Apartment = Trim(r.Apartment, 32),
                    OwnerId = owner.Id,
                    Status = UnitStatus.Unknown,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        if (errors.Count > 0)
            return new ImportResultDto(rows.Count, newOwners, newUnits, skipped, errors, false);

        if (commit) await db.SaveChangesAsync(ct);
        return new ImportResultDto(rows.Count, newOwners, newUnits, skipped, errors, commit);
    }

    private record SheetRow(int Number, string Name, string Phone, string Terrace, string Building, string Floor, string Apartment, string Construction);

    private static List<SheetRow> ReadRows(Stream file, out string? problem)
    {
        problem = null;
        try
        {
            using var wb = new XLWorkbook(file);
            var ws = wb.Worksheets.First();
            var last = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (last < 2)
            {
                problem = "الملف فارغ. اكتب بيانات الملاك تحت صف العناوين.";
                return [];
            }
            if (last - 1 > MaxRows)
            {
                problem = $"عدد الصفوف أكبر من {MaxRows}. قسّم الملف إلى أكثر من ملف.";
                return [];
            }

            var header = Cell(ws, 1, 1);
            if (header != Headers[0])
            {
                problem = $"عنوان العمود الأول يجب أن يكون «{Headers[0]}». حمّل النموذج الجاهز وانسخ بياناتك فيه.";
                return [];
            }

            var list = new List<SheetRow>();
            for (var r = 2; r <= last; r++)
            {
                var row = new SheetRow(r, Cell(ws, r, 1), Cell(ws, r, 2), Cell(ws, r, 3), Cell(ws, r, 4),
                    Cell(ws, r, 5), Cell(ws, r, 6), Cell(ws, r, 7));
                // Fully empty rows are ignored.
                if (new[] { row.Name, row.Phone, row.Terrace, row.Building, row.Floor, row.Apartment, row.Construction }
                    .All(string.IsNullOrWhiteSpace)) continue;
                list.Add(row);
            }
            if (list.Count == 0) problem = "الملف فارغ. اكتب بيانات الملاك تحت صف العناوين.";
            return list;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            problem = "تعذر قراءة الملف. تأكد أنه ملف Excel بصيغة xlsx وغير محمي بكلمة سر.";
            return [];
        }
    }

    private static string Cell(IXLWorksheet ws, int row, int col) => ws.Cell(row, col).GetString().Trim();

    private static string Key(string s) => string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static string UnitKey(string? construction, string? terrace, string? building, string? floor, string? apartment)
        => !string.IsNullOrWhiteSpace(construction)
            ? $"c:{Key(construction)}"
            : string.Join('|', "p", Key(terrace ?? ""), Key(building ?? ""), Key(floor ?? ""), Key(apartment ?? ""));

    private static string? Trim(string v, int max)
    {
        var t = v.Trim();
        return t.Length == 0 ? null : t.Length > max ? t[..max] : t;
    }
}
