using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Baymont.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options), IAppDbContext
{
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Contractor> Contractors => Set<Contractor>();
    public DbSet<ContractorWorker> ContractorWorkers => Set<ContractorWorker>();
    public DbSet<ContractorVehicle> ContractorVehicles => Set<ContractorVehicle>();
    public DbSet<ContractorEquipment> ContractorEquipment => Set<ContractorEquipment>();
    public DbSet<ContractorMaterial> ContractorMaterials => Set<ContractorMaterial>();
    public DbSet<ContractorEntry> ContractorEntries => Set<ContractorEntry>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<OvernightStay> OvernightStays => Set<OvernightStay>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<StaffEntry> StaffEntries => Set<StaffEntry>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationMovement> ReservationMovements => Set<ReservationMovement>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<DemoRecord> DemoRecords => Set<DemoRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot order or compare DateTimeOffset natively; store as UTC ticks.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToUtcTicksConverter>();
        configurationBuilder.Properties<DateTimeOffset?>()
            .HaveConversion<NullableDateTimeOffsetToUtcTicksConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Owner>(e =>
        {
            e.ToTable("owners");
            e.HasKey(x => x.Id);
            e.Property(x => x.InternalCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(256).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(64);
            e.Property(x => x.DeletedByUserId).HasMaxLength(64);
            e.HasIndex(x => x.InternalCode);
            e.HasIndex(x => x.FullName);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<Unit>(e =>
        {
            e.ToTable("units");
            e.HasKey(x => x.Id);
            e.Property(x => x.ConstructionNumber).HasMaxLength(64);
            e.Property(x => x.Terrace).HasMaxLength(64);
            e.Property(x => x.Building).HasMaxLength(64);
            e.Property(x => x.Floor).HasMaxLength(32);
            e.Property(x => x.Apartment).HasMaxLength(32);
            e.Property(x => x.DeletedByUserId).HasMaxLength(64);
            e.HasIndex(x => x.ConstructionNumber);
            e.HasIndex(x => new { x.Terrace, x.Building, x.Floor, x.Apartment });
            e.HasOne(x => x.Owner)
                .WithMany(o => o.Units)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<Vehicle>(e =>
        {
            e.ToTable("vehicles");
            e.HasKey(x => x.Id);
            e.Property(x => x.PlateNumber).HasMaxLength(64).IsRequired();
            e.Property(x => x.PlateNumberNormalized).HasMaxLength(64).IsRequired();
            e.Property(x => x.Make).HasMaxLength(64);
            e.Property(x => x.Model).HasMaxLength(64);
            e.Property(x => x.Color).HasMaxLength(64);
            e.Property(x => x.DeletedByUserId).HasMaxLength(64);
            // Soft-deleted plates can be reused; active plates stay unique.
            e.HasIndex(x => x.PlateNumberNormalized)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = 0");
            e.HasOne(x => x.Owner)
                .WithMany(o => o.Vehicles)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Unit)
                .WithMany(u => u.Vehicles)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<Contractor>(e =>
        {
            e.ToTable("contractors");
            e.HasKey(x => x.Id);
            e.Property(x => x.CompanyName).HasMaxLength(256).IsRequired();
            e.Property(x => x.ContractorName).HasMaxLength(256).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(64);
            e.Property(x => x.DeletedByUserId).HasMaxLength(64);
            e.HasIndex(x => x.CompanyName);
            e.HasIndex(x => x.ContractorName);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        ConfigureChild<ContractorWorker>(builder, "contractor_workers", c => c.Workers, e =>
        {
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.JobTitle).HasMaxLength(128);
            e.Property(x => x.Housing).HasMaxLength(256);
            e.Property(x => x.IdPhotoPath).HasMaxLength(128);
            e.Property(x => x.CriminalRecordPhotoPath).HasMaxLength(128);
        });
        ConfigureChild<ContractorVehicle>(builder, "contractor_vehicles", c => c.Vehicles, e =>
        {
            e.Property(x => x.PlateNumber).HasMaxLength(64).IsRequired();
            e.Property(x => x.PlateNumberNormalized).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.PlateNumberNormalized);
        });
        ConfigureChild<ContractorEquipment>(builder, "contractor_equipment", c => c.Equipment, e =>
        {
            e.Property(x => x.Number).HasMaxLength(64);
            e.Property(x => x.Type).HasMaxLength(128).IsRequired();
            e.Property(x => x.MovementPermit).HasMaxLength(128);
            e.Property(x => x.PhotoPath).HasMaxLength(128);
        });
        ConfigureChild<ContractorMaterial>(builder, "contractor_materials", c => c.Materials, e =>
        {
            e.Property(x => x.Description).HasMaxLength(512).IsRequired();
            e.Property(x => x.StorageLocation).HasMaxLength(256);
            e.Property(x => x.PhotoPath).HasMaxLength(128);
        });
        ConfigureChild<ContractorEntry>(builder, "contractor_entries", c => c.Entries, e =>
        {
            e.Property(x => x.EntryReason).HasMaxLength(512);
            e.Property(x => x.EntryPermit).HasMaxLength(128);
            e.Property(x => x.ExitReason).HasMaxLength(512);
            e.Property(x => x.ExitPermit).HasMaxLength(128);
        });

        builder.Entity<Delivery>(e =>
        {
            e.ToTable("deliveries");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitNumber).HasMaxLength(128).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            e.Property(x => x.HandoverEngineerName).HasMaxLength(256).IsRequired();
            e.Property(x => x.NotReceivedReason).HasMaxLength(1024);
            e.Property(x => x.DeletedByUserId).HasMaxLength(64);
            e.HasIndex(x => x.DeliveryDate);
            e.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.RowVersion).IsConcurrencyToken();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<OvernightStay>(e =>
        {
            e.ToTable("overnight_stays");
            e.HasKey(x => x.Id);
            e.Property(x => x.Notes).HasMaxLength(512);
            e.HasIndex(x => new { x.StayDate, x.Shift });
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.SetNull);
            // Hide stays whose owner was soft-deleted (matches the owner filter).
            e.HasQueryFilter(x => !x.Owner!.IsDeleted);
        });

        builder.Entity<StaffMember>(e =>
        {
            e.ToTable("staff_members");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.Company).HasMaxLength(256);
            e.Property(x => x.JobTitle).HasMaxLength(128);
            e.Property(x => x.PlateNumber).HasMaxLength(64);
            e.Property(x => x.Phone).HasMaxLength(64);
            e.HasIndex(x => x.Name);
        });

        builder.Entity<StaffEntry>(e =>
        {
            e.ToTable("staff_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Notes).HasMaxLength(512);
            e.HasIndex(x => x.EntryAt);
            e.HasOne(x => x.StaffMember).WithMany(s => s.Entries)
                .HasForeignKey(x => x.StaffMemberId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ShiftAssignment>(e =>
        {
            e.ToTable("shift_assignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.JobTitle).HasMaxLength(128);
            e.Property(x => x.AssignedWork).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(64);
            e.Property(x => x.Notes).HasMaxLength(512);
            e.HasIndex(x => new { x.ShiftDate, x.Kind });
        });

        builder.Entity<Reservation>(e =>
        {
            e.ToTable("reservations");
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitCode).HasMaxLength(128).IsRequired();
            e.Property(x => x.GuestNames).HasMaxLength(1024).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(1024);
            e.Property(x => x.ReviewedBy).HasMaxLength(128);
            e.Property(x => x.Source).HasMaxLength(16).IsRequired();
            e.Property(x => x.SourceFrom).HasMaxLength(256);
            e.Property(x => x.SourceSubject).HasMaxLength(512);
            e.Property(x => x.SourceFileName).HasMaxLength(256);
            e.Property(x => x.SourceMessageId).HasMaxLength(512);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CheckInDate);
            e.HasIndex(x => x.UnitCode);
            e.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ReservationMovement>(e =>
        {
            e.ToTable("reservation_movements");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ReservationId);
            e.HasOne(x => x.Reservation).WithMany(r => r.Movements)
                .HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DemoRecord>(e =>
        {
            e.ToTable("demo_records");
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.EntityType);
        });

        builder.Entity<InboxMessage>(e =>
        {
            e.ToTable("inbox_messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.MessageId).HasMaxLength(512).IsRequired();
            e.Property(x => x.FromAddress).HasMaxLength(256);
            e.Property(x => x.Subject).HasMaxLength(512);
            e.Property(x => x.Status).HasMaxLength(16).IsRequired();
            e.Property(x => x.Detail).HasMaxLength(1024);
            e.HasIndex(x => x.MessageId).IsUnique();
            e.HasIndex(x => x.ProcessedAt);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserName).HasMaxLength(128);
            e.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
            e.Property(x => x.Action).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });

        builder.Entity<AppUser>(e =>
        {
            e.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
        });
    }

    private static void ConfigureChild<T>(
        ModelBuilder builder,
        string table,
        System.Linq.Expressions.Expression<Func<Contractor, IEnumerable<T>?>> collection,
        Action<Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T>> configure)
        where T : ContractorChild
    {
        builder.Entity<T>(e =>
        {
            e.ToTable(table);
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Contractor)
                .WithMany(collection)
                .HasForeignKey(x => x.ContractorId)
                .OnDelete(DeleteBehavior.Cascade);
            // Match the parent's soft-delete filter.
            e.HasQueryFilter(x => !x.Contractor!.IsDeleted);
            configure(e);
        });
    }
}

public sealed class DateTimeOffsetToUtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
    v => v.UtcTicks,
    v => new DateTimeOffset(v, TimeSpan.Zero));

public sealed class NullableDateTimeOffsetToUtcTicksConverter() : ValueConverter<DateTimeOffset?, long?>(
    v => v == null ? null : v.Value.UtcTicks,
    v => v == null ? null : new DateTimeOffset(v.Value, TimeSpan.Zero));
