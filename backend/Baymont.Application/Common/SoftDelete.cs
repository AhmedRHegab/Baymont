using Baymont.Domain.Entities;

namespace Baymont.Application.Common;

public static class SoftDelete
{
    public static void Apply(ISoftDeletable entity, string? userId)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = userId;
    }
}
