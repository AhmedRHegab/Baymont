namespace Baymont.Domain.Entities;

/// <summary>
/// Remembers which records are sample data so they can be hard-deleted later.
/// One row with EntityType "Marker" stays forever so the sample data is never created twice.
/// </summary>
public class DemoRecord
{
    public const string MarkerType = "Marker";

    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
}
