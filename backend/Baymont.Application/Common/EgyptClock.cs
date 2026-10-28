namespace Baymont.Application.Common;

/// <summary>Dates as seen at the site (Egypt), and the Friday-to-Friday permit week.</summary>
public static class EgyptClock
{
    private static readonly TimeZoneInfo Zone = Resolve();

    private static TimeZoneInfo Resolve()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"); }
        catch (Exception) { return TimeZoneInfo.CreateCustomTimeZone("Egypt", TimeSpan.FromHours(2), "Egypt", "Egypt"); }
    }

    public static DateOnly DateOf(DateTimeOffset t) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(t, Zone).DateTime);

    public static DateOnly Today => DateOf(DateTimeOffset.UtcNow);

    /// <summary>The Friday closest to the date (an email sent on Saturday still belongs to the Friday before it).</summary>
    public static DateOnly NearestFriday(DateOnly d)
    {
        var back = ((int)d.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7;
        var forward = (7 - back) % 7;
        return back <= forward ? d.AddDays(-back) : d.AddDays(forward);
    }

    public static DateOnly FridayOnOrBefore(DateOnly d)
        => d.AddDays(-(((int)d.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7));

    private static readonly string[] Days = ["الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"];
    public static string ArabicDay(DateOnly d) => Days[(int)d.DayOfWeek];
}
