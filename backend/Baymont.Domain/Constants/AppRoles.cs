namespace Baymont.Domain.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string SearchOnly = "SearchOnly";
    /// <summary>Handover engineer: records deliveries, read-only elsewhere.</summary>
    public const string Overhead = "Overhead";

    public static readonly string[] All = [Admin, Editor, SearchOnly, Overhead];
}
