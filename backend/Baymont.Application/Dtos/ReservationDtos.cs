using Baymont.Domain.Enums;

namespace Baymont.Application.Dtos;

public record MovementDto(Guid Id, DateTimeOffset EnteredAt, DateTimeOffset? ExitedAt, bool IsFinal);

public record ReservationDto(
    Guid Id,
    string UnitCode,
    Guid? UnitId,
    string GuestNames,
    int PersonsCount,
    VisitorType? Type,
    DateOnly? CheckInDate,
    DateOnly? CheckOutDate,
    bool PeriodEstimated,
    string? Notes,
    ReservationStatus Status,
    bool IsInside,
    DateTimeOffset? FinalDepartureAt,
    IReadOnlyList<MovementDto> Movements,
    string Source,
    string? SourceFrom,
    string? SourceSubject,
    string? SourceFileName,
    DateTimeOffset? SourceSentAt,
    DateTimeOffset CreatedAt);

/// <summary>Create/update body. <paramref name="Approve"/> saves the reservation as approved after validating it.</summary>
public record SaveReservationRequest(
    string UnitCode,
    string GuestNames,
    int PersonsCount,
    VisitorType? Type,
    DateOnly? CheckInDate,
    DateOnly? CheckOutDate,
    string? Notes,
    bool Approve);

public record ReservationSource(
    string Kind, string? From, string? Subject, string? FileName, string? MessageId, DateTimeOffset? SentAt = null);

public record ReservationImportResultDto(
    int Created,
    int Duplicates,
    IReadOnlyList<ImportRowIssueDto> Issues);

public record CountPair(int Reservations, int Persons);

public record WeekRowDto(
    int Number,
    DateOnly Start,
    DateOnly NextStart,
    int Permits,
    int Authorized,
    int Arrived,
    int Departed,
    int Present,
    int Late);

public record ReservationDashboardDto(
    DateOnly Today,
    DateOnly WeekStart,
    int PendingReview,
    CountPair ThisWeekPermits,
    CountPair ThisWeekArrived,
    CountPair ThisWeekDeparted,
    CountPair PresentNow,
    CountPair Overdue,
    IReadOnlyList<ReservationDto> FollowUp,
    IReadOnlyList<WeekRowDto> Weeks);
