namespace HospitalQueue.Domain.Constants;

/// <summary>
/// Fixed set of screen/account types. Admin can still create additional
/// custom roles at runtime; these four are seeded because each maps to a
/// physical screen in the system (kiosk, display, employee call screen, admin).
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
    public const string Kiosk = "Kiosk";
    public const string Display = "Display";

    public static readonly string[] All = { Admin, Employee, Kiosk, Display };
}
