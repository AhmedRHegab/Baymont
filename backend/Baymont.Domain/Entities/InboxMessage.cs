namespace Baymont.Domain.Entities;

/// <summary>One email seen by the inbox reader. Also stops the same email from being processed twice.</summary>
public class InboxMessage
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string? FromAddress { get; set; }
    public string? Subject { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }

    /// <summary>Imported, Ignored or Failed.</summary>
    public string Status { get; set; } = "Ignored";
    public string? Detail { get; set; }
    public int PermitsCreated { get; set; }
}
