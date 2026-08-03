namespace HospitalQueue.Domain.Constants;

/// <summary>
/// Fine-grained permissions stored as ASP.NET Identity role claims
/// (claim type "permission"). Lets the admin screen grant/revoke individual
/// capabilities per role instead of only whole roles.
/// </summary>
public static class AppPermissions
{
    public const string ClaimType = "permission";

    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string ServicesManage = "services.manage";
    public const string CountersManage = "counters.manage";
    public const string TicketsCall = "tickets.call";
    public const string TicketsCreate = "tickets.create";
    public const string ReportsView = "reports.view";
    public const string DisplayView = "display.view";

    public static readonly string[] All =
    {
        UsersManage, RolesManage, ServicesManage, CountersManage,
        TicketsCall, TicketsCreate, ReportsView, DisplayView
    };
}
