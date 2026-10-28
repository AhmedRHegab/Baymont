using Baymont.Domain.Enums;

namespace Baymont.Domain.Entities;

/// <summary>
/// A visit permit (حجز) issued by leadership: one unit, one or more guests, a permit period
/// (normally Friday to Friday). Security records every entry and exit, and the final leave.
/// </summary>
public class Reservation
{
    public Guid Id { get; set; }

    /// <summary>The "Deal Name" from the permit sheet; matches a unit's construction number.</summary>
    public string UnitCode { get; set; } = string.Empty;
    public Guid? UnitId { get; set; }

    /// <summary>Guest names exactly as written in the sheet (several are joined with " / ").</summary>
    public string GuestNames { get; set; } = string.Empty;
    public int PersonsCount { get; set; } = 1;
    public VisitorType? Type { get; set; }
    public DateOnly? CheckInDate { get; set; }
    public DateOnly? CheckOutDate { get; set; }

    /// <summary>True while the period was guessed from the email's sent date and nobody has confirmed it.</summary>
    public bool PeriodEstimated { get; set; }
    public string? Notes { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.PendingReview;
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>
    /// Set when security records the final leave. After that the guest cannot enter again:
    /// a new permit (a new email) is needed.
    /// </summary>
    public DateTimeOffset? FinalDepartureAt { get; set; }

    /// <summary>Manual, Upload or Email.</summary>
    public string Source { get; set; } = "Manual";
    public string? SourceFrom { get; set; }
    public string? SourceSubject { get; set; }
    public string? SourceFileName { get; set; }
    public string? SourceMessageId { get; set; }

    /// <summary>When the permit email was sent.</summary>
    public DateTimeOffset? SourceSentAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Unit? Unit { get; set; }
    public ICollection<ReservationMovement> Movements { get; set; } = new List<ReservationMovement>();
}

/// <summary>One time the guests went in and (when they left) came out again during the permit.</summary>
public class ReservationMovement
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public DateTimeOffset EnteredAt { get; set; }
    public DateTimeOffset? ExitedAt { get; set; }

    /// <summary>True when this exit was the final leave.</summary>
    public bool IsFinal { get; set; }

    public Reservation? Reservation { get; set; }
}
