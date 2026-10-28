using Baymont.Application.Common;
using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

public class VehicleService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<VehicleDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await db.Vehicles
            .AsNoTracking()
            .Include(v => v.Owner)
            .Include(v => v.Unit)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException();

        return MapDetail(vehicle);
    }

    public async Task<VehicleDetailDto> CreateAsync(CreateVehicleRequest request, CancellationToken ct = default)
    {
        await EnsureLinks(request.OwnerId, request.UnitId, ct);
        var normalized = PlateNormalizer.Normalize(request.PlateNumber);
        if (string.IsNullOrEmpty(normalized))
            throw new ValidationException("رقم اللوحة مطلوب.");
        Check.MaxLength(request.PlateNumber, 64, "رقم اللوحة");

        var duplicate = await db.Vehicles.AnyAsync(v => v.PlateNumberNormalized == normalized, ct);
        if (duplicate)
            throw new ValidationException("رقم اللوحة مسجل مسبقاً.");

        var now = DateTimeOffset.UtcNow;
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            PlateNumber = request.PlateNumber.Trim(),
            PlateNumberNormalized = normalized,
            Make = NullIfWhiteSpace(request.Make),
            Model = NullIfWhiteSpace(request.Model),
            Color = NullIfWhiteSpace(request.Color),
            OwnerId = request.OwnerId,
            UnitId = request.UnitId,
            Notes = NullIfWhiteSpace(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Vehicles.Add(vehicle);
        await SaveAsync(ct);
        return await GetAsync(vehicle.Id, ct);
    }

    public async Task<VehicleDetailDto> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken ct = default)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException();

        if (vehicle.RowVersion != request.RowVersion)
            throw new ConcurrencyException();

        await EnsureLinks(request.OwnerId, request.UnitId, ct);
        var normalized = PlateNormalizer.Normalize(request.PlateNumber);
        if (string.IsNullOrEmpty(normalized))
            throw new ValidationException("رقم اللوحة مطلوب.");

        var duplicate = await db.Vehicles.AnyAsync(
            v => v.PlateNumberNormalized == normalized && v.Id != id, ct);
        if (duplicate)
            throw new ValidationException("رقم اللوحة مسجل مسبقاً.");

        vehicle.PlateNumber = request.PlateNumber.Trim();
        vehicle.PlateNumberNormalized = normalized;
        vehicle.Make = NullIfWhiteSpace(request.Make);
        vehicle.Model = NullIfWhiteSpace(request.Model);
        vehicle.Color = NullIfWhiteSpace(request.Color);
        vehicle.OwnerId = request.OwnerId;
        vehicle.UnitId = request.UnitId;
        vehicle.Notes = NullIfWhiteSpace(request.Notes);
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;

        await SaveAsync(ct);
        return await GetAsync(vehicle.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NotFoundException();
        SoftDelete.Apply(vehicle, currentUser.UserId);
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveAsync(ct);
    }

    private async Task EnsureLinks(Guid? ownerId, Guid? unitId, CancellationToken ct)
    {
        if (ownerId is not null && !await db.Owners.AnyAsync(o => o.Id == ownerId, ct))
            throw new NotFoundException("المالك غير موجود.");
        if (unitId is not null && !await db.Units.AnyAsync(u => u.Id == unitId, ct))
            throw new NotFoundException("الوحدة غير موجودة.");
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

    private static VehicleDetailDto MapDetail(Vehicle v) => new(
        v.Id,
        v.PlateNumber,
        v.Make,
        v.Model,
        v.Color,
        v.OwnerId,
        v.Owner?.FullName,
        v.UnitId,
        v.Unit?.ConstructionNumber,
        v.Notes,
        v.RowVersion);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
