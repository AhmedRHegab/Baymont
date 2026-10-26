using Baymont.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Owner> Owners { get; }
    DbSet<Unit> Units { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Contractor> Contractors { get; }
    DbSet<ContractorWorker> ContractorWorkers { get; }
    DbSet<ContractorVehicle> ContractorVehicles { get; }
    DbSet<ContractorEquipment> ContractorEquipment { get; }
    DbSet<ContractorMaterial> ContractorMaterials { get; }
    DbSet<ContractorEntry> ContractorEntries { get; }
    DbSet<Delivery> Deliveries { get; }
    DbSet<OvernightStay> OvernightStays { get; }
    DbSet<StaffMember> StaffMembers { get; }
    DbSet<StaffEntry> StaffEntries { get; }
    DbSet<ShiftAssignment> ShiftAssignments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AppUser> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
