namespace Baymont.Application.Dtos;

public record VehicleSummaryDto(
    Guid Id,
    string PlateNumber,
    string? Make,
    string? Model,
    string? Color,
    Guid? OwnerId,
    string? OwnerName,
    Guid? UnitId,
    string? ConstructionNumber);

public record VehicleDetailDto(
    Guid Id,
    string PlateNumber,
    string? Make,
    string? Model,
    string? Color,
    Guid? OwnerId,
    string? OwnerName,
    Guid? UnitId,
    string? ConstructionNumber,
    string? Notes,
    uint RowVersion);

public record CreateVehicleRequest(
    string PlateNumber,
    string? Make,
    string? Model,
    string? Color,
    Guid? OwnerId,
    Guid? UnitId,
    string? Notes);

public record UpdateVehicleRequest(
    string PlateNumber,
    string? Make,
    string? Model,
    string? Color,
    Guid? OwnerId,
    Guid? UnitId,
    string? Notes,
    uint RowVersion);
