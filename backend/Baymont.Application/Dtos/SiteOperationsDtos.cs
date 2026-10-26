using Baymont.Domain.Enums;

namespace Baymont.Application.Dtos;

// ---- Overnight owners ----
public record SaveOvernightStayRequest(
    DateOnly StayDate, ShiftPeriod Shift, Guid OwnerId, Guid? UnitId, string? Notes);

public record OvernightItemDto(
    Guid Id, Guid OwnerId, string OwnerName, Guid? UnitId,
    string? Building, string? Floor, string? Apartment, string? Notes);

public record OvernightGroupDto(string Terrace, IReadOnlyList<OvernightItemDto> Items);

public record OvernightReportDto(
    DateOnly Date, ShiftPeriod Shift, int Total, IReadOnlyList<OvernightGroupDto> Groups);

// ---- Staff ----
public record SaveStaffRequest(string Name, string? Company, string? JobTitle, string? PlateNumber, string? Phone);

public record StaffDto(
    Guid Id, string Name, string? Company, string? JobTitle, string? PlateNumber, string? Phone,
    bool IsOnSite, DateTimeOffset? LastEntryAt, DateTimeOffset? LastExitAt);

public record StaffLogDto(
    Guid Id, Guid StaffMemberId, string Name, string? Company, string? JobTitle,
    DateTimeOffset EntryAt, DateTimeOffset? ExitAt);

public record OnSiteStaffDto(
    Guid Id, string Name, string? Company, string? JobTitle, string? PlateNumber, string? Phone,
    DateTimeOffset EntryAt);

public record OnSiteContractorDto(
    Guid Id, string CompanyName, string ContractorName, int WorkerCount, DateOnly? EntryDate, string? EntryReason);

public record OnSiteDto(
    IReadOnlyList<OnSiteStaffDto> Staff,
    IReadOnlyList<OnSiteContractorDto> Contractors,
    int ContractorWorkers);

// ---- Shift roster ----
public record SaveShiftAssignmentRequest(
    DateOnly ShiftDate, ShiftKind Kind, string Name, string? JobTitle,
    string? AssignedWork, string? Phone, string? Notes);

public record ShiftAssignmentDto(
    Guid Id, DateOnly ShiftDate, ShiftKind Kind, string Name, string? JobTitle,
    string? AssignedWork, string? Phone, string? Notes);
