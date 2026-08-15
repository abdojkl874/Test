namespace HospitalQueue.Domain.Entities;

/// <summary>Single-row settings table (Id is always 1) edited from the admin "الإعدادات العامة" screen.</summary>
public class OrganizationSettings
{
    public int Id { get; set; } = 1;
    public string OrgNameAr { get; set; } = "نظام إدارة طابور المشفى";
    public bool TickerEnabled { get; set; }
    public string? TickerText { get; set; }

    /// <summary>Hashed shared password gating /kiosk. Null = screen not yet configured, access blocked.</summary>
    public string? KioskPasswordHash { get; set; }

    /// <summary>Hashed shared password gating /display. Null = screen not yet configured, access blocked.</summary>
    public string? DisplayPasswordHash { get; set; }

    /// <summary>Organization logo as a data: URI, shown on the login, admin, kiosk and display screens.</summary>
    public string? LogoDataUrl { get; set; }

    /// <summary>Brand primary colour (hex). Overrides the --hq-primary family at runtime; null keeps the built-in palette.</summary>
    public string? BrandPrimaryColor { get; set; }

    /// <summary>Brand accent colour (hex). Overrides the --hq-accent family at runtime; null keeps the built-in palette.</summary>
    public string? BrandAccentColor { get; set; }
}
