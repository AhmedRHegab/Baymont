using Baymont.Domain.Enums;

namespace Baymont.Domain.Entities;

public class Contractor : ISoftDeletable
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContractorName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Assignment { get; set; }
    public DateOnly? WorkStartDate { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    public ICollection<ContractorWorker> Workers { get; set; } = new List<ContractorWorker>();
    public ICollection<ContractorVehicle> Vehicles { get; set; } = new List<ContractorVehicle>();
    public ICollection<ContractorEquipment> Equipment { get; set; } = new List<ContractorEquipment>();
    public ICollection<ContractorMaterial> Materials { get; set; } = new List<ContractorMaterial>();
    public ICollection<ContractorEntry> Entries { get; set; } = new List<ContractorEntry>();
}

public abstract class ContractorChild
{
    public Guid Id { get; set; }
    public Guid ContractorId { get; set; }
    public Contractor? Contractor { get; set; }
}

public class ContractorWorker : ContractorChild
{
    public string Name { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Housing { get; set; }
    public string? IdPhotoPath { get; set; }
    public string? CriminalRecordPhotoPath { get; set; }
}

public class ContractorVehicle : ContractorChild
{
    public string PlateNumber { get; set; } = string.Empty;
    public string PlateNumberNormalized { get; set; } = string.Empty;
    public bool IsSiteServiceCar { get; set; }
    public string? Notes { get; set; }
}

public class ContractorEquipment : ContractorChild
{
    public string? Number { get; set; }
    public string Type { get; set; } = string.Empty;
    public EquipmentCategory Category { get; set; } = EquipmentCategory.Light;
    public string? MovementPermit { get; set; }
    public string? PhotoPath { get; set; }
    public string? Notes { get; set; }
}

public class ContractorMaterial : ContractorChild
{
    public string Description { get; set; } = string.Empty;
    public string? StorageLocation { get; set; }
    public string? PhotoPath { get; set; }
}

/// <summary>One site entry (and its exit) of a contractor: dates, reasons and permits.</summary>
public class ContractorEntry : ContractorChild
{
    public DateOnly? EntryDate { get; set; }
    public string? EntryReason { get; set; }
    public string? EntryPermit { get; set; }
    public DateOnly? ExitDate { get; set; }
    public string? ExitReason { get; set; }
    public string? ExitPermit { get; set; }
}
