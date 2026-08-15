using HospitalQueue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueue.Web.Services;

/// <summary>The organization's visual identity, as edited from /admin/settings.</summary>
public record Branding(string OrgName, string? LogoDataUrl, string? PrimaryColor, string? AccentColor)
{
    public static readonly Branding Default = new("نظام إدارة طابور المشفى", null, null, null);
}

/// <summary>
/// Loads branding straight from OrganizationSettings and caches it in memory:
/// the logo is a data URI that would otherwise be re-read from the database on
/// every page render, and it only changes when an admin saves the settings.
/// </summary>
public class BrandingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Branding? _cached;

    public BrandingService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<Branding> GetAsync(CancellationToken ct = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_cached is not null)
            {
                return _cached;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var settings = await db.OrganizationSettings.AsNoTracking().FirstOrDefaultAsync(ct);

            _cached = settings is null
                ? Branding.Default
                : new Branding(settings.OrgNameAr, settings.LogoDataUrl, settings.BrandPrimaryColor, settings.BrandAccentColor);

            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Called after an admin saves settings so the next read picks the new values up.</summary>
    public void Invalidate() => _cached = null;

    /// <summary>
    /// Re-points the palette's CSS variables at the organization's colours.
    /// Returns null when nothing is configured, leaving site.css's built-in
    /// palette in place so an unbranded install still looks finished.
    /// </summary>
    public async Task<string?> GetCssVariablesAsync(CancellationToken ct = default)
    {
        var branding = await GetAsync(ct);
        var rules = new List<string>();

        if (IsHex(branding.PrimaryColor))
        {
            rules.Add($"--hq-primary:{branding.PrimaryColor};");
            rules.Add($"--hq-primary-dark:{Shade(branding.PrimaryColor!, -0.22)};");
            rules.Add($"--hq-primary-soft:{Shade(branding.PrimaryColor!, 0.18)};");
        }

        if (IsHex(branding.AccentColor))
        {
            rules.Add($"--hq-accent:{branding.AccentColor};");
            rules.Add($"--hq-accent-soft:{Shade(branding.AccentColor!, 0.25)};");
        }

        return rules.Count == 0 ? null : $":root{{{string.Join(string.Empty, rules)}}}";
    }

    /// <summary>Guards against anything but a plain hex colour reaching the stylesheet.</summary>
    private static bool IsHex(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        System.Text.RegularExpressions.Regex.IsMatch(value, "^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$");

    /// <summary>Lightens (positive) or darkens (negative) a hex colour to derive the dark/soft variants.</summary>
    private static string Shade(string hex, double amount)
    {
        var value = hex.TrimStart('#');
        if (value.Length == 3)
        {
            value = string.Concat(value.Select(c => $"{c}{c}"));
        }

        var r = Convert.ToInt32(value[..2], 16);
        var g = Convert.ToInt32(value.Substring(2, 2), 16);
        var b = Convert.ToInt32(value.Substring(4, 2), 16);

        return $"#{Adjust(r, amount):X2}{Adjust(g, amount):X2}{Adjust(b, amount):X2}";
    }

    private static int Adjust(int channel, double amount)
    {
        var target = amount < 0 ? 0 : 255;
        return (int)Math.Round(channel + (target - channel) * Math.Abs(amount));
    }
}
