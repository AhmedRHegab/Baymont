using Baymont.Domain.Enums;

namespace Baymont.Domain.Entities;

public class Unit : ISoftDeletable
{
    public Guid Id { get; set; }
    public string? ConstructionNumber { get; set; }
    public string? Terrace { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public Guid? OwnerId { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Unknown;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    public Owner? Owner { get; set; }
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
