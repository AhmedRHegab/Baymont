namespace Baymont.Domain.Entities;

/// <summary>
/// Remembers which records are sample data so they can be hard-deleted later.
/// Rows whose EntityType starts with "Marker" are bookkeeping, not sample data, and are never deleted:
/// "Marker" means the sample data was created once, "Marker:Reset:&lt;token&gt;" means that reset already ran.
/// </summary>
public class DemoRecord
{
    public const string MarkerType = "Marker";

    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
}
