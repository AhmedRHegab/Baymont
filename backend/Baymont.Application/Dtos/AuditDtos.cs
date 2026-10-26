namespace Baymont.Application.Dtos;

public record AuditLogDto(
    Guid Id,
    string? UserId,
    string? UserName,
    string EntityType,
    string EntityId,
    string Action,
    DateTimeOffset OccurredAt,
    string? OldValuesJson,
    string? NewValuesJson,
    string? IpAddress);

