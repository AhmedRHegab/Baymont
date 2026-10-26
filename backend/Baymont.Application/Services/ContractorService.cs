using Baymont.Application.Common;
using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Baymont.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

public class ContractorService(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<ContractorSummaryDto>> ListAsync(string? query, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim().ToLowerInvariant();
        var contractors = db.Contractors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            contractors = contractors.Where(c =>
                c.CompanyName.ToLower().Contains(q) ||
                c.ContractorName.ToLower().Contains(q) ||
                (c.Phone != null && c.Phone.ToLower().Contains(q)));
        }

        return await contractors
            .OrderBy(c => c.CompanyName)
            .Take(100)
            .Select(c => new ContractorSummaryDto(c.Id, c.CompanyName, c.ContractorName, c.Phone, c.Workers.Count))
            .ToListAsync(ct);
    }

    public async Task<ContractorDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var c = await db.Contractors
            .AsNoTracking()
            .Include(x => x.Workers)
            .Include(x => x.Vehicles)
            .Include(x => x.Equipment)
            .Include(x => x.Materials)
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException();

        return MapDetail(c);
    }

    public async Task<ContractorDetailDto> CreateAsync(SaveContractorRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var contractor = new Contractor
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };
        ApplyScalars(contractor, request);
        ApplyChildren(contractor, request);

        db.Contractors.Add(contractor);
        await SaveAsync(ct);
        return await GetAsync(contractor.Id, ct);
    }

    public async Task<ContractorDetailDto> UpdateAsync(Guid id, SaveContractorRequest request, CancellationToken ct = default)
    {
        var contractor = await db.Contractors
            .Include(x => x.Workers)
            .Include(x => x.Vehicles)
            .Include(x => x.Equipment)
            .Include(x => x.Materials)
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException();

        if (contractor.RowVersion != request.RowVersion)
            throw new ConcurrencyException();

        ApplyScalars(contractor, request);
        ApplyChildren(contractor, request);
        // Always changes, so the parent row is saved and its version token moves on child-only edits.
        contractor.UpdatedAt = DateTimeOffset.UtcNow;

        await SaveAsync(ct);
        return await GetAsync(contractor.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var contractor = await db.Contractors.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException();
        SoftDelete.Apply(contractor, currentUser.UserId);
        contractor.UpdatedAt = DateTimeOffset.UtcNow;
        await SaveAsync(ct);
    }

    private static void ApplyScalars(Contractor c, SaveContractorRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.CompanyName))
            throw new ArgumentException("اسم الشركة مطلوب.");
        if (string.IsNullOrWhiteSpace(r.ContractorName))
            throw new ArgumentException("اسم المقاول مطلوب.");

        c.CompanyName = r.CompanyName.Trim();
        c.ContractorName = r.ContractorName.Trim();
        c.Phone = NullIfWhiteSpace(r.Phone);
        c.Assignment = NullIfWhiteSpace(r.Assignment);
        c.WorkStartDate = r.WorkStartDate;
        c.Notes = NullIfWhiteSpace(r.Notes);
    }

    private void ApplyChildren(Contractor c, SaveContractorRequest r)
    {
        Sync(c.Workers, r.Workers, d => d.Id,
            d =>
            {
                if (string.IsNullOrWhiteSpace(d.Name)) throw new ArgumentException("اسم العامل مطلوب.");
            },
            (e, d) =>
            {
                e.Name = d.Name.Trim();
                e.JobTitle = NullIfWhiteSpace(d.JobTitle);
                e.Housing = NullIfWhiteSpace(d.Housing);
                e.IdPhotoPath = UploadNames.Clean(d.IdPhotoPath);
                e.CriminalRecordPhotoPath = UploadNames.Clean(d.CriminalRecordPhotoPath);
            });

        Sync(c.Vehicles, r.Vehicles, d => d.Id,
            d =>
            {
                if (string.IsNullOrEmpty(PlateNormalizer.Normalize(d.PlateNumber)))
                    throw new ArgumentException("رقم اللوحة مطلوب.");
            },
            (e, d) =>
            {
                e.PlateNumber = d.PlateNumber.Trim();
                e.PlateNumberNormalized = PlateNormalizer.Normalize(d.PlateNumber);
                e.IsSiteServiceCar = d.IsSiteServiceCar;
                e.Notes = NullIfWhiteSpace(d.Notes);
            });

        Sync(c.Equipment, r.Equipment, d => d.Id,
            d =>
            {
                if (string.IsNullOrWhiteSpace(d.Type)) throw new ArgumentException("نوع المعدة مطلوب.");
                if (!Enum.IsDefined(d.Category)) throw new ArgumentException("تصنيف المعدة غير صالح.");
            },
            (e, d) =>
            {
                e.Number = NullIfWhiteSpace(d.Number);
                e.Type = d.Type.Trim();
                e.Category = d.Category;
                e.MovementPermit = NullIfWhiteSpace(d.MovementPermit);
                e.PhotoPath = UploadNames.Clean(d.PhotoPath);
                e.Notes = NullIfWhiteSpace(d.Notes);
            });

        Sync(c.Materials, r.Materials, d => d.Id,
            d =>
            {
                if (string.IsNullOrWhiteSpace(d.Description)) throw new ArgumentException("وصف الخامة مطلوب.");
            },
            (e, d) =>
            {
                e.Description = d.Description.Trim();
                e.StorageLocation = NullIfWhiteSpace(d.StorageLocation);
                e.PhotoPath = UploadNames.Clean(d.PhotoPath);
            });

        Sync(c.Entries, r.Entries, d => d.Id,
            _ => { },
            (e, d) =>
            {
                e.EntryDate = d.EntryDate;
                e.EntryReason = NullIfWhiteSpace(d.EntryReason);
                e.EntryPermit = NullIfWhiteSpace(d.EntryPermit);
                e.ExitDate = d.ExitDate;
                e.ExitReason = NullIfWhiteSpace(d.ExitReason);
                e.ExitPermit = NullIfWhiteSpace(d.ExitPermit);
            });
    }

    /// <summary>
    /// Makes <paramref name="existing"/> match <paramref name="incoming"/>: unknown ids are
    /// added, listed ids are updated, missing ones removed. A null list leaves it untouched.
    /// </summary>
    private static void Sync<TEntity, TDto>(
        ICollection<TEntity> existing,
        IReadOnlyList<TDto>? incoming,
        Func<TDto, Guid?> idOf,
        Action<TDto> validate,
        Action<TEntity, TDto> apply)
        where TEntity : ContractorChild, new()
    {
        if (incoming is null) return;

        foreach (var dto in incoming) validate(dto);

        var keep = incoming.Select(idOf).Where(i => i != null).Select(i => i!.Value).ToHashSet();
        foreach (var gone in existing.Where(e => !keep.Contains(e.Id)).ToList())
            existing.Remove(gone);

        foreach (var dto in incoming)
        {
            var id = idOf(dto);
            var entity = id is null ? null : existing.FirstOrDefault(e => e.Id == id);
            if (entity is null)
            {
                entity = new TEntity { Id = Guid.NewGuid() };
                apply(entity, dto);
                existing.Add(entity);
            }
            else
            {
                apply(entity, dto);
            }
        }
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

    private static ContractorDetailDto MapDetail(Contractor c) => new(
        c.Id,
        c.CompanyName,
        c.ContractorName,
        c.Phone,
        c.Assignment,
        c.WorkStartDate,
        c.Notes,
        c.RowVersion,
        c.Workers.OrderBy(x => x.Name)
            .Select(x => new ContractorWorkerDto(x.Id, x.Name, x.JobTitle, x.Housing, x.IdPhotoPath, x.CriminalRecordPhotoPath))
            .ToList(),
        c.Vehicles.OrderBy(x => x.PlateNumber)
            .Select(x => new ContractorVehicleDto(x.Id, x.PlateNumber, x.IsSiteServiceCar, x.Notes))
            .ToList(),
        c.Equipment.OrderBy(x => x.Type)
            .Select(x => new ContractorEquipmentDto(x.Id, x.Number, x.Type, x.Category, x.MovementPermit, x.PhotoPath, x.Notes))
            .ToList(),
        c.Materials.OrderBy(x => x.Description)
            .Select(x => new ContractorMaterialDto(x.Id, x.Description, x.StorageLocation, x.PhotoPath))
            .ToList(),
        c.Entries.OrderByDescending(x => x.EntryDate)
            .Select(x => new ContractorEntryDto(x.Id, x.EntryDate, x.EntryReason, x.EntryPermit, x.ExitDate, x.ExitReason, x.ExitPermit))
            .ToList());

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
