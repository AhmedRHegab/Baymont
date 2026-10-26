using Baymont.Domain.Enums;

namespace Baymont.Application.Dtos;

public record ContractorSummaryDto(
    Guid Id,
    string CompanyName,
    string ContractorName,
    string? Phone,
    int WorkerCount);

public record ContractorWorkerDto(
    Guid? Id,
    string Name,
    string? JobTitle,
    string? Housing,
    string? IdPhotoPath,
    string? CriminalRecordPhotoPath);

public record ContractorVehicleDto(
    Guid? Id,
    string PlateNumber,
    bool IsSiteServiceCar,
    string? Notes);

public record ContractorEquipmentDto(
    Guid? Id,
    string? Number,
    string Type,
    EquipmentCategory Category,
    string? MovementPermit,
    string? PhotoPath,
    string? Notes);

public record ContractorMaterialDto(
    Guid? Id,
    string Description,
    string? StorageLocation,
    string? PhotoPath);

public record ContractorEntryDto(
    Guid? Id,
    DateOnly? EntryDate,
    string? EntryReason,
    string? EntryPermit,
    DateOnly? ExitDate,
    string? ExitReason,
    string? ExitPermit);

public record ContractorDetailDto(
    Guid Id,
    string CompanyName,
    string ContractorName,
    string? Phone,
    string? Assignment,
    DateOnly? WorkStartDate,
    string? Notes,
    uint RowVersion,
    IReadOnlyList<ContractorWorkerDto> Workers,
    IReadOnlyList<ContractorVehicleDto> Vehicles,
    IReadOnlyList<ContractorEquipmentDto> Equipment,
    IReadOnlyList<ContractorMaterialDto> Materials,
    IReadOnlyList<ContractorEntryDto> Entries);

/// <summary>Create/update body. Child lists replace the stored ones (matched by Id).</summary>
public record SaveContractorRequest(
    string CompanyName,
    string ContractorName,
    string? Phone,
    string? Assignment,
    DateOnly? WorkStartDate,
    string? Notes,
    uint RowVersion,
    IReadOnlyList<ContractorWorkerDto>? Workers,
    IReadOnlyList<ContractorVehicleDto>? Vehicles,
    IReadOnlyList<ContractorEquipmentDto>? Equipment,
    IReadOnlyList<ContractorMaterialDto>? Materials,
    IReadOnlyList<ContractorEntryDto>? Entries);
