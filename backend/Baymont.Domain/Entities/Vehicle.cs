namespace Baymont.Domain.Entities;

public class Vehicle : ISoftDeletable
{
    public Guid Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string PlateNumberNormalized { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? UnitId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    public Owner? Owner { get; set; }
    public Unit? Unit { get; set; }
}
