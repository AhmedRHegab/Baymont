using System.Text.Json;
using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Baymont.Infrastructure.Interceptors;

public class AuditSaveChangesInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    private static readonly HashSet<string> AuditedTypes =
    [
        nameof(Owner),
        nameof(Unit),
        nameof(Vehicle),
        nameof(Delivery),
        nameof(Reservation),
        nameof(ReservationMovement),
        nameof(OvernightStay),
        nameof(StaffMember),
        nameof(StaffEntry),
        nameof(ShiftAssignment),
        nameof(Contractor),
        nameof(ContractorWorker),
        nameof(ContractorVehicle),
        nameof(ContractorEquipment),
        nameof(ContractorMaterial),
        nameof(ContractorEntry)
    ];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        WriteAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        WriteAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void WriteAuditEntries(DbContext? context)
    {
        if (context is null) return;

        // SQLite has no row-version column, so bump the concurrency token ourselves.
        foreach (var e in context.ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Modified && e.Metadata.FindProperty("RowVersion") != null))
        {
            var rv = e.Property("RowVersion");
            rv.CurrentValue = unchecked((uint)rv.OriginalValue! + 1);
        }

        var now = DateTimeOffset.UtcNow;
        var entries = context.ChangeTracker.Entries()
            .Where(e =>
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                AuditedTypes.Contains(e.Entity.GetType().Name))
            .ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType().Name;
            var entityId = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue?.ToString()
                           ?? string.Empty;

            var isSoftDelete = entry.State == EntityState.Modified
                && entry.Entity is ISoftDeletable soft
                && soft.IsDeleted
                && entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified
                && (bool?)entry.Property(nameof(ISoftDeletable.IsDeleted)).OriginalValue == false;

            var action = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Deleted => "Delete",
                EntityState.Modified when isSoftDelete => "Delete",
                EntityState.Modified => "Update",
                _ => entry.State.ToString()
            };

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = currentUser.UserId,
                UserName = currentUser.UserName,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                OccurredAt = now,
                OldValuesJson = entry.State is EntityState.Modified or EntityState.Deleted
                    ? Serialize(entry, useOriginal: true)
                    : null,
                NewValuesJson = entry.State is EntityState.Added or EntityState.Modified
                    ? Serialize(entry, useOriginal: false)
                    : null,
                IpAddress = currentUser.IpAddress
            };

            context.Set<AuditLog>().Add(audit);
        }
    }

    private static string Serialize(EntityEntry entry, bool useOriginal)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.Name is "RowVersion") continue;
            dict[prop.Metadata.Name] = useOriginal ? prop.OriginalValue : prop.CurrentValue;
        }

        return JsonSerializer.Serialize(dict);
    }
}
