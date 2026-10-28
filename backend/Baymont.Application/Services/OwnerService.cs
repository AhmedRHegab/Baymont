using Baymont.Application.Common;
using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

public class OwnerService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<OwnerSummaryDto>> LookupAsync(string? query, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim().ToLowerInvariant();
        var owners = db.Owners.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            owners = owners.Where(o =>
                o.FullName.ToLower().Contains(q) ||
                o.InternalCode.ToLower().Contains(q) ||
                (o.Phone != null && o.Phone.ToLower().Contains(q)));
        }

        return await owners
            .OrderBy(o => o.FullName)
            .Take(30)
            .Select(o => new OwnerSummaryDto(o.Id, o.InternalCode, o.FullName, o.Phone))
            .ToListAsync(ct);
    }

    public async Task<OwnerDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var owner = await db.Owners
            .AsNoTracking()
            .Include(o => o.Units)
            .Include(o => o.Vehicles)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException();

        return MapDetail(owner);
    }

    public async Task<OwnerDetailDto> CreateAsync(CreateOwnerRequest request, CancellationToken ct = default)
    {
        Check.Required(request.FullName, "اسم المالك");
        Check.Phone(request.Phone, "رقم الموبايل");
        var code = string.IsNullOrWhiteSpace(request.InternalCode)
            // The random tail keeps two owners created in the same second from getting the same code.
            ? $"OWN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}"
            : request.InternalCode.Trim();
        await EnsureCodeUnique(code, Guid.Empty, ct);

        var now = DateTimeOffset.UtcNow;
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            InternalCode = code,
            FullName = request.FullName.Trim(),
            Phone = NullIfWhiteSpace(request.Phone),
            Notes = NullIfWhiteSpace(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Owners.Add(owner);
        await SaveAsync(ct);
        return await GetAsync(owner.Id, ct);
    }

    public async Task<OwnerDetailDto> UpdateAsync(Guid id, UpdateOwnerRequest request, CancellationToken ct = default)
    {
        var owner = await db.Owners.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException();

        if (owner.RowVersion != request.RowVersion)
            throw new ConcurrencyException();

        Check.Required(request.FullName, "اسم المالك");
        Check.Phone(request.Phone, "رقم الموبايل");
        var newCode = string.IsNullOrWhiteSpace(request.InternalCode) ? owner.InternalCode : request.InternalCode.Trim();
        if (!string.Equals(newCode, owner.InternalCode, StringComparison.OrdinalIgnoreCase))
            await EnsureCodeUnique(newCode, owner.Id, ct);
        owner.InternalCode = newCode;
        owner.FullName = request.FullName.Trim();
        owner.Phone = NullIfWhiteSpace(request.Phone);
        owner.Notes = NullIfWhiteSpace(request.Notes);
        owner.UpdatedAt = DateTimeOffset.UtcNow;

        await SaveAsync(ct);
        return await GetAsync(owner.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var owner = await db.Owners.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException();
        SoftDelete.Apply(owner, currentUser.UserId);
        owner.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveAsync(ct);
    }

    private async Task EnsureCodeUnique(string code, Guid exceptId, CancellationToken ct)
    {
        var lower = code.ToLowerInvariant();
        if (await db.Owners.AnyAsync(o => o.InternalCode.ToLower() == lower && o.Id != exceptId, ct))
            throw new ValidationException("الرقم الداخلي مستخدم لمالك آخر. اختر رقماً مختلفاً أو اتركه فارغاً.");
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException();
        }
    }

    private static OwnerDetailDto MapDetail(Owner owner) => new(
        owner.Id,
        owner.InternalCode,
        owner.FullName,
        owner.Phone,
        owner.Notes,
        owner.RowVersion,
        owner.Units.Select(u => new UnitSummaryDto(
            u.Id, u.ConstructionNumber, u.Terrace, u.Building, u.Floor, u.Apartment,
            u.Status, owner.Id, owner.FullName)).ToList(),
        owner.Vehicles.Select(v => new VehicleSummaryDto(
            v.Id, v.PlateNumber, v.Make, v.Model, v.Color,
            owner.Id, owner.FullName, v.UnitId, null)).ToList());

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
