using Baymont.Application.Common;
using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

public class UnitService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<UnitSummaryDto>> LookupAsync(string? query, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim().ToLowerInvariant();
        var units = db.Units.AsNoTracking().Include(u => u.Owner).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            units = units.Where(u =>
                (u.ConstructionNumber != null && u.ConstructionNumber.ToLower().Contains(q)) ||
                (u.Terrace != null && u.Terrace.ToLower().Contains(q)) ||
                (u.Building != null && u.Building.ToLower().Contains(q)) ||
                (u.Apartment != null && u.Apartment.ToLower().Contains(q)) ||
                (u.Owner != null && u.Owner.FullName.ToLower().Contains(q)));
        }

        return await units
            .OrderBy(u => u.ConstructionNumber)
            .Take(30)
            .Select(u => new UnitSummaryDto(
                u.Id,
                u.ConstructionNumber,
                u.Terrace,
                u.Building,
                u.Floor,
                u.Apartment,
                u.Status,
                u.OwnerId,
                u.Owner != null ? u.Owner.FullName : null))
            .ToListAsync(ct);
    }

    public async Task<UnitDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var unit = await db.Units
            .AsNoTracking()
            .Include(u => u.Owner)
            .Include(u => u.Vehicles)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException();

        return MapDetail(unit);
    }

    public async Task<UnitDetailDto> CreateAsync(CreateUnitRequest request, CancellationToken ct = default)
    {
        await EnsureOwnerExists(request.OwnerId, ct);

        var now = DateTimeOffset.UtcNow;
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            ConstructionNumber = NullIfWhiteSpace(request.ConstructionNumber),
            Terrace = NullIfWhiteSpace(request.Terrace),
            Building = NullIfWhiteSpace(request.Building),
            Floor = NullIfWhiteSpace(request.Floor),
            Apartment = NullIfWhiteSpace(request.Apartment),
            Status = request.Status,
            OwnerId = request.OwnerId,
            Notes = NullIfWhiteSpace(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Units.Add(unit);
        await SaveAsync(ct);
        return await GetAsync(unit.Id, ct);
    }

    public async Task<UnitDetailDto> UpdateAsync(Guid id, UpdateUnitRequest request, CancellationToken ct = default)
    {
        var unit = await db.Units.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException();

        if (unit.RowVersion != request.RowVersion)
            throw new ConcurrencyException();

        await EnsureOwnerExists(request.OwnerId, ct);

        unit.ConstructionNumber = NullIfWhiteSpace(request.ConstructionNumber);
        unit.Terrace = NullIfWhiteSpace(request.Terrace);
        unit.Building = NullIfWhiteSpace(request.Building);
        unit.Floor = NullIfWhiteSpace(request.Floor);
        unit.Apartment = NullIfWhiteSpace(request.Apartment);
        unit.Status = request.Status;
        unit.OwnerId = request.OwnerId;
        unit.Notes = NullIfWhiteSpace(request.Notes);
        unit.UpdatedAt = DateTimeOffset.UtcNow;

        await SaveAsync(ct);
        return await GetAsync(unit.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var unit = await db.Units.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException();
        SoftDelete.Apply(unit, currentUser.UserId);
        unit.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveAsync(ct);
    }

    private async Task EnsureOwnerExists(Guid? ownerId, CancellationToken ct)
    {
        if (ownerId is null) return;
        var exists = await db.Owners.AnyAsync(o => o.Id == ownerId, ct);
        if (!exists) throw new NotFoundException("المالك غير موجود.");
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

    private static UnitDetailDto MapDetail(Unit unit) => new(
        unit.Id,
        unit.ConstructionNumber,
        unit.Terrace,
        unit.Building,
        unit.Floor,
        unit.Apartment,
        unit.Status,
        unit.OwnerId,
        unit.Owner?.FullName,
        unit.Notes,
        unit.RowVersion,
        unit.Vehicles.Select(v => new VehicleSummaryDto(
            v.Id, v.PlateNumber, v.Make, v.Model, v.Color,
            v.OwnerId, unit.Owner?.FullName, unit.Id, unit.ConstructionNumber)).ToList());

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
