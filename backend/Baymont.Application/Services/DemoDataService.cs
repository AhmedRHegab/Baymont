using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

public record DemoStatusDto(bool HasDemoData, int Records);

/// <summary>Reports on and permanently removes the sample data created on first start.</summary>
public class DemoDataService(IAppDbContext db)
{
    public async Task<DemoStatusDto> StatusAsync(CancellationToken ct = default)
    {
        var count = await db.DemoRecords.CountAsync(d => d.EntityType != DemoRecord.MarkerType, ct);
        return new DemoStatusDto(count > 0, count);
    }

    /// <summary>Hard-deletes every sample record, including their audit-log lines. Real data is untouched.</summary>
    public async Task<int> ClearAsync(CancellationToken ct = default)
    {
        var rows = await db.DemoRecords.AsNoTracking()
            .Where(d => d.EntityType != DemoRecord.MarkerType)
            .Select(d => new { d.EntityType, d.EntityId })
            .ToListAsync(ct);
        if (rows.Count == 0) return 0;

        List<Guid> Ids(string type) => rows.Where(r => r.EntityType == type).Select(r => r.EntityId).ToList();

        // ExecuteDelete skips the soft-delete behaviour and the audit interceptor. Children go with their
        // parent through the database's cascade rules; links from real records to sample ones are set to null.
        var reservations = Ids(nameof(Reservation));
        await db.Reservations.Where(x => reservations.Contains(x.Id)).ExecuteDeleteAsync(ct);

        var deliveries = Ids(nameof(Delivery));
        await db.Deliveries.IgnoreQueryFilters().Where(x => deliveries.Contains(x.Id)).ExecuteDeleteAsync(ct);

        var stays = Ids(nameof(OvernightStay));
        await db.OvernightStays.IgnoreQueryFilters().Where(x => stays.Contains(x.Id)).ExecuteDeleteAsync(ct);

        var inbox = Ids(nameof(InboxMessage));
        await db.InboxMessages.Where(x => inbox.Contains(x.Id)).ExecuteDeleteAsync(ct);

        var shifts = Ids(nameof(ShiftAssignment));
        await db.ShiftAssignments.Where(x => shifts.Contains(x.Id)).ExecuteDeleteAsync(ct);

        var staff = Ids(nameof(StaffMember));
        await db.StaffMembers.Where(x => staff.Contains(x.Id)).ExecuteDeleteAsync(ct);

        var contractors = Ids(nameof(Contractor));
        await db.Contractors.IgnoreQueryFilters().Where(x => contractors.Contains(x.Id)).ExecuteDeleteAsync(ct);

        var vehicles = Ids(nameof(Vehicle));
        await db.Vehicles.IgnoreQueryFilters().Where(x => vehicles.Contains(x.Id)).ExecuteDeleteAsync(ct);

        var units = Ids(nameof(Unit));
        await db.Units.IgnoreQueryFilters().Where(x => units.Contains(x.Id)).ExecuteDeleteAsync(ct);

        var owners = Ids(nameof(Owner));
        await db.Owners.IgnoreQueryFilters().Where(x => owners.Contains(x.Id)).ExecuteDeleteAsync(ct);

        // The audit log lines that creating the sample data produced.
        var auditIds = rows.Select(r => r.EntityId.ToString()).ToList();
        await db.AuditLogs.Where(a => auditIds.Contains(a.EntityId)).ExecuteDeleteAsync(ct);

        await db.DemoRecords.Where(d => d.EntityType != DemoRecord.MarkerType).ExecuteDeleteAsync(ct);
        return rows.Count;
    }
}
