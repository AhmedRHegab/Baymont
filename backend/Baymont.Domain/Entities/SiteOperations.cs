using Baymont.Domain.Enums;

namespace Baymont.Domain.Entities;

/// <summary>An owner who stayed overnight on site, per day and shift.</summary>
public class OvernightStay
{
    public Guid Id { get; set; }
    public DateOnly StayDate { get; set; }
    public ShiftPeriod Shift { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? UnitId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Owner? Owner { get; set; }
    public Unit? Unit { get; set; }
}

/// <summary>An employee of our own company who enters and leaves the site.</summary>
public class StaffMember
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? PlateNumber { get; set; }
    public string? Phone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<StaffEntry> Entries { get; set; } = new List<StaffEntry>();
}

public class StaffEntry
{
    public Guid Id { get; set; }
    public Guid StaffMemberId { get; set; }
    public DateTimeOffset EntryAt { get; set; }
    public DateTimeOffset? ExitAt { get; set; }
    public string? Notes { get; set; }

    public StaffMember? StaffMember { get; set; }
}

/// <summary>One person on a day's outside shift or night patrol.</summary>
public class ShiftAssignment
{
    public Guid Id { get; set; }
    public DateOnly ShiftDate { get; set; }
    public ShiftKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? AssignedWork { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
