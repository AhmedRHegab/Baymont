using Baymont.Application.Common;
using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Baymont.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

/// <summary>Overnight owners, staff entry/exit, the on-site report and the shift roster.</summary>
public class SiteOperationsService(IAppDbContext db)
{
    private const string NoTerrace = "بدون مصطبة";

    // ---------- Overnight owners ----------

    public async Task<OvernightReportDto> OvernightAsync(DateOnly date, ShiftPeriod shift, CancellationToken ct = default)
    {
        var rows = await db.OvernightStays
            .AsNoTracking()
            .Where(s => s.StayDate == date && s.Shift == shift)
            .Select(s => new
            {
                s.Id, s.OwnerId, OwnerName = s.Owner!.FullName, s.UnitId, s.Notes, s.CreatedAt,
                Terrace = s.Unit != null ? s.Unit.Terrace : null,
                Building = s.Unit != null ? s.Unit.Building : null,
                Floor = s.Unit != null ? s.Unit.Floor : null,
                Apartment = s.Unit != null ? s.Unit.Apartment : null
            })
            .ToListAsync(ct);

        var groups = rows
            .OrderBy(r => r.CreatedAt)
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Terrace) ? NoTerrace : r.Terrace!)
            .OrderBy(g => g.Key == NoTerrace ? 1 : 0)
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new OvernightGroupDto(
                g.Key,
                g.Select(r => new OvernightItemDto(
                    r.Id, r.OwnerId, r.OwnerName, r.UnitId, r.Building, r.Floor, r.Apartment, r.Notes)).ToList()))
            .ToList();

        return new OvernightReportDto(date, shift, rows.Count, groups);
    }

    public async Task<Guid> AddOvernightAsync(SaveOvernightStayRequest r, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(r.Shift)) throw new ValidationException("الوردية غير صالحة.");
        Check.Date(r.StayDate, "التاريخ");
        if (!await db.Owners.AnyAsync(o => o.Id == r.OwnerId, ct))
            throw new NotFoundException("المالك غير موجود.");
        if (r.UnitId is not null && !await db.Units.AnyAsync(u => u.Id == r.UnitId, ct))
            throw new NotFoundException("الوحدة غير موجودة.");
        if (await db.OvernightStays.AnyAsync(
                s => s.StayDate == r.StayDate && s.Shift == r.Shift && s.OwnerId == r.OwnerId, ct))
            throw new ValidationException("هذا المالك مسجل مسبقاً في نفس اليوم والوردية.");

        var stay = new OvernightStay
        {
            Id = Guid.NewGuid(),
            StayDate = r.StayDate,
            Shift = r.Shift,
            OwnerId = r.OwnerId,
            UnitId = r.UnitId,
            Notes = Clean(r.Notes),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.OvernightStays.Add(stay);
        await db.SaveChangesAsync(ct);
        return stay.Id;
    }

    public async Task DeleteOvernightAsync(Guid id, CancellationToken ct = default)
    {
        var stay = await db.OvernightStays.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException();
        db.OvernightStays.Remove(stay);
        await db.SaveChangesAsync(ct);
    }

    // ---------- Staff ----------

    public async Task<IReadOnlyList<StaffDto>> StaffAsync(string? query, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim().ToLowerInvariant();
        var staff = db.StaffMembers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            staff = staff.Where(s =>
                s.Name.ToLower().Contains(q) ||
                (s.Company != null && s.Company.ToLower().Contains(q)) ||
                (s.PlateNumber != null && s.PlateNumber.ToLower().Contains(q)) ||
                (s.Phone != null && s.Phone.ToLower().Contains(q)));
        }

        var rows = await staff
            .OrderBy(s => s.Name)
            .Take(300)
            .Select(s => new
            {
                s.Id, s.Name, s.Company, s.JobTitle, s.PlateNumber, s.Phone,
                LastEntry = s.Entries.OrderByDescending(e => e.EntryAt).FirstOrDefault()
            })
            .ToListAsync(ct);

        return rows.Select(r => new StaffDto(
            r.Id, r.Name, r.Company, r.JobTitle, r.PlateNumber, r.Phone,
            r.LastEntry is { ExitAt: null }, r.LastEntry?.EntryAt, r.LastEntry?.ExitAt)).ToList();
    }

    public async Task<Guid> CreateStaffAsync(SaveStaffRequest r, CancellationToken ct = default)
    {
        var member = new StaffMember { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        ApplyStaff(member, r);
        db.StaffMembers.Add(member);
        await db.SaveChangesAsync(ct);
        return member.Id;
    }

    public async Task UpdateStaffAsync(Guid id, SaveStaffRequest r, CancellationToken ct = default)
    {
        var member = await db.StaffMembers.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException();
        ApplyStaff(member, r);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteStaffAsync(Guid id, CancellationToken ct = default)
    {
        var member = await db.StaffMembers.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException();
        db.StaffMembers.Remove(member);
        await db.SaveChangesAsync(ct);
    }

    public async Task EnterAsync(Guid staffId, CancellationToken ct = default)
    {
        if (!await db.StaffMembers.AnyAsync(s => s.Id == staffId, ct)) throw new NotFoundException();
        if (await db.StaffEntries.AnyAsync(e => e.StaffMemberId == staffId && e.ExitAt == null, ct))
            throw new ValidationException("الموظف مسجل دخول بالفعل.");

        db.StaffEntries.Add(new StaffEntry
        {
            Id = Guid.NewGuid(),
            StaffMemberId = staffId,
            EntryAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task ExitAsync(Guid staffId, CancellationToken ct = default)
    {
        var open = await db.StaffEntries
            .Where(e => e.StaffMemberId == staffId && e.ExitAt == null)
            .OrderByDescending(e => e.EntryAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new ValidationException("الموظف غير مسجل دخول.");
        open.ExitAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Entries that overlap the given local day (Egypt is a fixed UTC+2/+3, so the caller sends UTC bounds).</summary>
    public async Task<IReadOnlyList<StaffLogDto>> StaffLogAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        return await db.StaffEntries
            .AsNoTracking()
            .Where(e => e.EntryAt < to && (e.ExitAt == null || e.ExitAt >= from))
            .OrderByDescending(e => e.EntryAt)
            .Take(500)
            .Select(e => new StaffLogDto(
                e.Id, e.StaffMemberId, e.StaffMember!.Name, e.StaffMember.Company, e.StaffMember.JobTitle,
                e.EntryAt, e.ExitAt))
            .ToListAsync(ct);
    }

    public async Task<OnSiteDto> OnSiteAsync(CancellationToken ct = default)
    {
        var staff = await db.StaffEntries
            .AsNoTracking()
            .Where(e => e.ExitAt == null)
            .OrderBy(e => e.StaffMember!.Name)
            .Select(e => new OnSiteStaffDto(
                e.StaffMemberId, e.StaffMember!.Name, e.StaffMember.Company, e.StaffMember.JobTitle,
                e.StaffMember.PlateNumber, e.StaffMember.Phone, e.EntryAt))
            .ToListAsync(ct);

        // A contractor is on site while it has an entry with a date but no exit date.
        var contractors = await db.ContractorEntries
            .AsNoTracking()
            .Where(e => e.EntryDate != null && e.ExitDate == null)
            .OrderBy(e => e.Contractor!.CompanyName)
            .Select(e => new OnSiteContractorDto(
                e.ContractorId, e.Contractor!.CompanyName, e.Contractor.ContractorName,
                e.Contractor.Workers.Count, e.EntryDate, e.EntryReason))
            .ToListAsync(ct);

        // A contractor with several open entries must only be counted once.
        contractors = contractors.GroupBy(c => c.Id).Select(g => g.OrderByDescending(c => c.EntryDate).First()).ToList();

        // Guests with a permit who are inside right now (they entered and have not walked out).
        var guests = await db.ReservationMovements
            .AsNoTracking()
            .Where(m => m.ExitedAt == null && m.Reservation!.Status == ReservationStatus.Approved)
            .OrderBy(m => m.Reservation!.GuestNames)
            .Select(m => new OnSiteGuestDto(
                m.ReservationId, m.Reservation!.GuestNames, m.Reservation.UnitCode, m.Reservation.Type,
                m.Reservation.PersonsCount, m.EnteredAt))
            .ToListAsync(ct);

        var workers = contractors.Sum(c => c.WorkerCount);
        var guestPersons = guests.Sum(g => g.PersonsCount);
        return new OnSiteDto(staff, contractors, workers, guests, guestPersons, staff.Count + workers + guestPersons);
    }

    // ---------- Shift roster ----------

    public async Task<IReadOnlyList<ShiftAssignmentDto>> ShiftsAsync(DateOnly date, CancellationToken ct = default)
        => await db.ShiftAssignments
            .AsNoTracking()
            .Where(s => s.ShiftDate == date)
            .OrderBy(s => s.Kind).ThenBy(s => s.CreatedAt)
            .Select(s => new ShiftAssignmentDto(
                s.Id, s.ShiftDate, s.Kind, s.Name, s.JobTitle, s.AssignedWork, s.Phone, s.Notes))
            .ToListAsync(ct);

    public async Task<Guid> CreateShiftAsync(SaveShiftAssignmentRequest r, CancellationToken ct = default)
    {
        var a = new ShiftAssignment { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        ApplyShift(a, r);
        db.ShiftAssignments.Add(a);
        await db.SaveChangesAsync(ct);
        return a.Id;
    }

    public async Task UpdateShiftAsync(Guid id, SaveShiftAssignmentRequest r, CancellationToken ct = default)
    {
        var a = await db.ShiftAssignments.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException();
        ApplyShift(a, r);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteShiftAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.ShiftAssignments.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException();
        db.ShiftAssignments.Remove(a);
        await db.SaveChangesAsync(ct);
    }

    // ---------- helpers ----------

    private static void ApplyStaff(StaffMember m, SaveStaffRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) throw new ValidationException("اسم الموظف مطلوب.");
        Check.Phone(r.Phone, "رقم الموبايل");
        m.Name = r.Name.Trim();
        m.Company = Clean(r.Company);
        m.JobTitle = Clean(r.JobTitle);
        m.PlateNumber = Clean(r.PlateNumber);
        m.Phone = Clean(r.Phone);
    }

    private static void ApplyShift(ShiftAssignment a, SaveShiftAssignmentRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) throw new ValidationException("الاسم مطلوب.");
        if (!Enum.IsDefined(r.Kind)) throw new ValidationException("نوع الوردية غير صالح.");
        Check.Date(r.ShiftDate, "اليوم");
        Check.Phone(r.Phone, "رقم التليفون");
        a.ShiftDate = r.ShiftDate;
        a.Kind = r.Kind;
        a.Name = r.Name.Trim();
        a.JobTitle = Clean(r.JobTitle);
        a.AssignedWork = Clean(r.AssignedWork);
        a.Phone = Clean(r.Phone);
        a.Notes = Clean(r.Notes);
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
