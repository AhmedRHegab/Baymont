using Baymont.Domain.Enums;

namespace Baymont.Application.Dtos;

public record UnitSummaryDto(
    Guid Id,
    string? ConstructionNumber,
    string? Terrace,
    string? Building,
    string? Floor,
    string? Apartment,
    UnitStatus Status,
    Guid? OwnerId,
    string? OwnerName);

public record UnitDetailDto(
    Guid Id,
    string? ConstructionNumber,
    string? Terrace,
    string? Building,
    string? Floor,
    string? Apartment,
    UnitStatus Status,
    Guid? OwnerId,
    string? OwnerName,
    string? Notes,
    uint RowVersion,
    IReadOnlyList<VehicleSummaryDto> Vehicles);

public record CreateUnitRequest(
    string? ConstructionNumber,
    string? Terrace,
    string? Building,
    string? Floor,
    string? Apartment,
    UnitStatus Status,
    Guid? OwnerId,
    string? Notes);

public record UpdateUnitRequest(
    string? ConstructionNumber,
    string? Terrace,
    string? Building,
    string? Floor,
    string? Apartment,
    UnitStatus Status,
    Guid? OwnerId,
    string? Notes,
    uint RowVersion);
