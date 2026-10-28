using Baymont.Application.Common;
using Baymont.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Baymont.Infrastructure.Interceptors;

/// <summary>
/// The last line of defence before anything reaches the database: required text may not be blank and no text
/// may be longer than its limit. SQLite itself ignores length limits, so without this a 3,000-character name
/// would simply be stored. Applies to every business record, whoever wrote the code that saves it.
/// </summary>
public class ValidationSaveChangesInterceptor : SaveChangesInterceptor
{
    /// <summary>Text fields with no explicit limit in the model still get this cap (notes, comments...).</summary>
    private const int DefaultMaxLength = 2000;

    private static readonly HashSet<string> Checked =
    [
        nameof(Owner), nameof(Unit), nameof(Vehicle), nameof(Delivery), nameof(OvernightStay),
        nameof(Contractor), nameof(ContractorWorker), nameof(ContractorVehicle), nameof(ContractorEquipment),
        nameof(ContractorMaterial), nameof(ContractorEntry), nameof(StaffMember), nameof(StaffEntry),
        nameof(ShiftAssignment), nameof(Reservation), nameof(AppUser)
    ];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Validate(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Validate(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Validate(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            var type = entry.Entity.GetType().Name;
            if (!Checked.Contains(type)) continue;

            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.ClrType != typeof(string)) continue;
                if (entry.State == EntityState.Modified && !prop.IsModified) continue;
                // Of the Identity user only our own display name is ours to check.
                if (type == nameof(AppUser) && prop.Metadata.Name != nameof(AppUser.DisplayName)) continue;

                var name = prop.Metadata.Name;
                var value = prop.CurrentValue as string;
                var label = FieldLabels.Get(type, name);

                if (!prop.Metadata.IsNullable && string.IsNullOrWhiteSpace(value))
                    throw new ValidationException($"الحقل «{label}» مطلوب.");

                var max = prop.Metadata.GetMaxLength() ?? DefaultMaxLength;
                if (value is not null && value.Length > max)
                    throw new ValidationException($"الحقل «{label}» طويل. الحد الأقصى {max} حرفاً.");
            }
        }
    }
}
