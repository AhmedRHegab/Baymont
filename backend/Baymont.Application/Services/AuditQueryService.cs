using Baymont.Application.Dtos;
using Baymont.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Application.Services;

public class AuditQueryService(IAppDbContext db)
{
    public async Task<IReadOnlyList<AuditLogDto>> GetAsync(
        string? entityType,
        string? entityId,
        string? userId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int take = 100,
        CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(a => a.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId);
        if (from is not null)
            query = query.Where(a => a.OccurredAt >= from);
        if (to is not null)
            query = query.Where(a => a.OccurredAt <= to);

        var logs = await query
            .OrderByDescending(a => a.OccurredAt)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(ct);

        var userIds = logs
            .Select(a => a.UserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        var users = await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.DisplayName })
            .ToListAsync(ct);

        var byId = users.ToDictionary(u => u.Id);

        return logs.Select(a =>
        {
            string? name = a.UserName;
            if (a.UserId is not null && byId.TryGetValue(a.UserId, out var user))
            {
                name = !string.IsNullOrWhiteSpace(user.DisplayName)
                    ? user.DisplayName
                    : user.UserName;
            }

            return new AuditLogDto(
                a.Id,
                a.UserId,
                name,
                a.EntityType,
                a.EntityId,
                a.Action,
                a.OccurredAt,
                a.OldValuesJson,
                a.NewValuesJson,
                a.IpAddress);
        }).ToList();
    }
}
