using HospitalQueue.Domain.Constants;
using HospitalQueue.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HospitalQueue.Web.Pages.Account;

/// <summary>
/// Lightweight sign-in for shared terminal devices (kiosk / public display):
/// the operator taps the device's account instead of typing a username, then
/// enters a short PIN on an on-screen keypad. Underneath it's the exact same
/// ASP.NET Core Identity sign-in as the full login page — only the input UX
/// (and the fact the "password" is a short PIN) differs.
/// </summary>
[AllowAnonymous]
public class PinLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public PinLoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public record PinAccountOption(string Username, string FullName);

    public List<PinAccountOption> Accounts { get; set; } = new();

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Pin { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync() => await LoadAccountsAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Pin))
        {
            ErrorMessage = "الرجاء اختيار الجهاز وإدخال الرمز السري";
            await LoadAccountsAsync();
            return Page();
        }

        var user = await _userManager.FindByNameAsync(Username);
        if (user is null || !user.IsActive)
        {
            ErrorMessage = "الرمز غير صحيح";
            await LoadAccountsAsync();
            return Page();
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(AppRoles.Kiosk) && !roles.Contains(AppRoles.Display))
        {
            ErrorMessage = "هذا الحساب غير مخصص لدخول الأجهزة";
            await LoadAccountsAsync();
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user, Pin, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ErrorMessage = result.IsLockedOut
                ? "تم قفل الجهاز مؤقتاً بسبب محاولات خاطئة متكررة، حاول لاحقاً"
                : "الرمز غير صحيح";
            await LoadAccountsAsync();
            return Page();
        }

        var home = roles.Contains(AppRoles.Kiosk) ? "/kiosk" : "/display";
        return LocalRedirect(home);
    }

    private async Task LoadAccountsAsync()
    {
        var kiosk = await _userManager.GetUsersInRoleAsync(AppRoles.Kiosk);
        var display = await _userManager.GetUsersInRoleAsync(AppRoles.Display);

        Accounts = kiosk.Concat(display)
            .Where(u => u.IsActive)
            .DistinctBy(u => u.Id)
            .Select(u => new PinAccountOption(u.UserName!, u.FullName))
            .OrderBy(a => a.FullName)
            .ToList();
    }
}
