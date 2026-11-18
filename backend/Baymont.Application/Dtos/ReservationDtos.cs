using Baymont.Domain.Enums;

namespace Baymont.Application.Dtos;

public record GuestDto(
    Guid Id,
    string Name,
    /// <summary>The full number for admins and editors; masked ("**********5678") for everyone else.</summary>
    string NationalId,
    DateOnly? BirthDate,
    PersonGender? Gender,
    string? Governorate,
    bool HasPhoto,
    string? IdPhotoPath,
    DateTimeOffset CreatedAt);

public record SaveGuestRequest(string Name, string NationalId, string? IdPhotoPath);

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
    IReadOnlyList<GuestDto> Guests,
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
