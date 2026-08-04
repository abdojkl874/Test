using MudBlazor;

namespace HospitalQueue.Web.Shared;

/// <summary>
/// Deliberately a different palette family from a typical green/gold hospital
/// theme: deep navy as the brand color with a warm amber accent for calls to
/// action, plus a muted plum as a third chart color. Font family (Cairo) is
/// applied via CSS in site.css rather than MudBlazor's Typography object, to
/// keep this theme file small and low-risk.
/// </summary>
public static class AppTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#123B5E",
            Secondary = "#E0793F",
            Tertiary = "#6D4C91",
            Background = "#F4F6F8",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#14202B",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#14202B",
            TextPrimary = "#14202B",
            TextSecondary = "#5B6B7A",
            Success = "#1F8A5F",
            Warning = "#B7791F",
            Error = "#C0392B",
            Info = "#2C7BA6",
            Divider = "#E4E9ED",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
        },
    };
}
