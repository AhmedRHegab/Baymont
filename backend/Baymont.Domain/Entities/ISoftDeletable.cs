namespace Baymont.Domain.Entities;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedByUserId { get; set; }
}
