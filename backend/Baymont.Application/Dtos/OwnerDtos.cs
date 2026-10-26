namespace Baymont.Application.Dtos;

public record OwnerSummaryDto(
    Guid Id,
    string InternalCode,
    string FullName,
    string? Phone);

public record OwnerDetailDto(
    Guid Id,
    string InternalCode,
    string FullName,
    string? Phone,
    string? Notes,
    uint RowVersion,
    IReadOnlyList<UnitSummaryDto> Units,
    IReadOnlyList<VehicleSummaryDto> Vehicles);

public record CreateOwnerRequest(
    string? InternalCode,
    string FullName,
    string? Phone,
    string? Notes);

public record UpdateOwnerRequest(
    string? InternalCode,
    string FullName,
    string? Phone,
    string? Notes,
    uint RowVersion);
