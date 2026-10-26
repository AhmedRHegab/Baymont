namespace Baymont.Domain.Entities;

public class Owner : ISoftDeletable
{
    public Guid Id { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
