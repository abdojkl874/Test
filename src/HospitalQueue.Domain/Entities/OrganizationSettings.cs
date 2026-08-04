namespace HospitalQueue.Domain.Entities;

/// <summary>Single-row settings table (Id is always 1) edited from the admin "الإعدادات العامة" screen.</summary>
public class OrganizationSettings
{
    public int Id { get; set; } = 1;
    public string OrgNameAr { get; set; } = "نظام إدارة طابور المشفى";
    public bool TickerEnabled { get; set; }
    public string? TickerText { get; set; }
}
