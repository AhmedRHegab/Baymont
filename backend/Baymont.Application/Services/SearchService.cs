using Baymont.Application.Common;
using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

public class SearchService(IAppDbContext db)
{
    public async Task<SearchResponse> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(q))
            return new SearchResponse(q, []);

        var normalizedPlate = PlateNormalizer.Normalize(q);
        var lower = q.ToLowerInvariant();
        var hits = new List<SearchHitDto>();

        var vehicles = await db.Vehicles
            .AsNoTracking()
            .Include(v => v.Owner)
            .Include(v => v.Unit)
            .Where(v =>
                v.PlateNumberNormalized == normalizedPlate ||
                v.PlateNumber.ToLower().Contains(lower) ||
                (normalizedPlate != "" && v.PlateNumberNormalized.Contains(normalizedPlate)))
            .Take(25)
            .ToListAsync(ct);

        foreach (var v in vehicles)
        {
            var exact = v.PlateNumberNormalized == normalizedPlate;
            hits.Add(new SearchHitDto(
                "vehicle",
                v.Id,
                v.PlateNumber,
                BuildVehicleSubtitle(v.Owner?.FullName, v.Unit?.ConstructionNumber),
                exact ? 100 : 80));
        }

        var units = await db.Units
            .AsNoTracking()
            .Include(u => u.Owner)
            .Where(u =>
                (u.ConstructionNumber != null && u.ConstructionNumber == q) ||
                (u.ConstructionNumber != null && u.ConstructionNumber.ToLower().Contains(lower)) ||
                (u.Apartment != null && u.Apartment.ToLower().Contains(lower)) ||
                (u.Building != null && u.Building.ToLower().Contains(lower)))
            .Take(25)
            .ToListAsync(ct);

        foreach (var u in units)
        {
            var exact = string.Equals(u.ConstructionNumber, q, StringComparison.OrdinalIgnoreCase);
            hits.Add(new SearchHitDto(
                "unit",
                u.Id,
                u.ConstructionNumber ?? $"{u.Terrace}/{u.Building}/{u.Floor}/{u.Apartment}",
                u.Owner?.FullName,
                exact ? 95 : 70));
        }

        var owners = await db.Owners
            .AsNoTracking()
            .Where(o =>
                o.FullName.ToLower().Contains(lower) ||
                o.InternalCode.ToLower().Contains(lower) ||
                (o.Phone != null && o.Phone.ToLower().Contains(lower)))
            .Take(25)
            .ToListAsync(ct);

        foreach (var o in owners)
        {
            hits.Add(new SearchHitDto(
                "owner",
                o.Id,
                o.FullName,
                o.InternalCode,
                60));
        }

        var contractors = await db.Contractors
            .AsNoTracking()
            .Where(c =>
                c.CompanyName.ToLower().Contains(lower) ||
                c.ContractorName.ToLower().Contains(lower) ||
                (c.Phone != null && c.Phone.ToLower().Contains(lower)))
            .Take(25)
            .ToListAsync(ct);

        foreach (var c in contractors)
            hits.Add(new SearchHitDto("contractor", c.Id, c.CompanyName, c.ContractorName, 65));

        // Contractor workers and their cars lead back to the contractor.
        var viaWorkers = await db.ContractorWorkers
            .AsNoTracking()
            .Where(w => w.Name.ToLower().Contains(lower))
            .Select(w => new { w.ContractorId, w.Contractor!.CompanyName, w.Name })
            .Take(25)
            .ToListAsync(ct);

        foreach (var w in viaWorkers)
            hits.Add(new SearchHitDto("contractor", w.ContractorId, w.CompanyName, $"عامل: {w.Name}", 55));

        var viaCars = await db.ContractorVehicles
            .AsNoTracking()
            .Where(v => normalizedPlate != "" && v.PlateNumberNormalized.Contains(normalizedPlate))
            .Select(v => new { v.ContractorId, v.Contractor!.CompanyName, v.PlateNumber, v.PlateNumberNormalized })
            .Take(25)
            .ToListAsync(ct);

        foreach (var v in viaCars)
            hits.Add(new SearchHitDto(
                "contractor",
                v.ContractorId,
                v.CompanyName,
                $"سيارة مقاول: {v.PlateNumber}",
                v.PlateNumberNormalized == normalizedPlate ? 98 : 78));

        var staff = await db.StaffMembers
            .AsNoTracking()
            .Where(s =>
                s.Name.ToLower().Contains(lower) ||
                (s.Company != null && s.Company.ToLower().Contains(lower)) ||
                (s.Phone != null && s.Phone.ToLower().Contains(lower)) ||
                (s.PlateNumber != null && normalizedPlate != "" && s.PlateNumber.ToLower().Contains(lower)))
            .Take(25)
            .ToListAsync(ct);

        foreach (var s in staff)
        {
            var exact = !string.IsNullOrEmpty(s.PlateNumber) && PlateNormalizer.Normalize(s.PlateNumber) == normalizedPlate;
            hits.Add(new SearchHitDto(
                "staff",
                s.Id,
                s.Name,
                string.Join(" · ", new[] { s.Company, s.JobTitle, s.PlateNumber }.Where(x => !string.IsNullOrWhiteSpace(x))),
                exact ? 97 : 58));
        }

        var deliveries = await db.Deliveries
            .AsNoTracking()
            .Where(d =>
                d.UnitNumber.ToLower().Contains(lower) ||
                d.CustomerName.ToLower().Contains(lower))
            .OrderByDescending(d => d.DeliveryDate)
            .Take(25)
            .ToListAsync(ct);

        foreach (var d in deliveries)
            hits.Add(new SearchHitDto(
                "delivery",
                d.Id,
                $"تسليم وحدة {d.UnitNumber}",
                $"{d.CustomerName} · {d.DeliveryDate:yyyy-MM-dd} · {(d.IsReceived ? "تم الاستلام" : "لم يتم")}",
                50));

        var ranked = hits
            .OrderByDescending(h => h.Rank)
            .ThenBy(h => h.Title)
            .Take(50)
            .ToList();

        return new SearchResponse(q, ranked);
    }

    private static string? BuildVehicleSubtitle(string? ownerName, string? constructionNumber)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(ownerName)) parts.Add(ownerName);
        if (!string.IsNullOrWhiteSpace(constructionNumber)) parts.Add($"وحدة {constructionNumber}");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }
}
