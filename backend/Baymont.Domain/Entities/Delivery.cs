namespace Baymont.Domain.Entities;

/// <summary>Handover of a unit to its customer, recorded by the handover engineer.</summary>
public class Delivery : ISoftDeletable
{
    public Guid Id { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public Guid? UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string HandoverEngineerName { get; set; } = string.Empty;
    public bool IsReceived { get; set; }
    public string? NotReceivedReason { get; set; }
    public bool IsFurnished { get; set; }
    public string? Comments { get; set; }
    public string? Complaints { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    public Unit? Unit { get; set; }
}
