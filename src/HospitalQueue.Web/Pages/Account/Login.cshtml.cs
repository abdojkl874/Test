using System.ComponentModel.DataAnnotations;
using HospitalQueue.Domain.Constants;
using HospitalQueue.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using HospitalQueue.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HospitalQueue.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BrandingService _branding;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        BrandingService branding)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _branding = branding;
    }

    public Branding Brand { get; private set; } = HospitalQueue.Web.Services.Branding.Default;

    [BindProperty]
    [Required(ErrorMessage = "الرجاء إدخال اسم المستخدم")]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "الرجاء إدخال كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync() => Brand = await _branding.GetAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        Brand = await _branding.GetAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByNameAsync(Username);
        if (user is null || !user.IsActive)
        {
            ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user, Password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ErrorMessage = result.IsLockedOut
                ? "تم قفل الحساب مؤقتاً بسبب محاولات دخول خاطئة متكررة، حاول لاحقاً"
                : "اسم المستخدم أو كلمة المرور غير صحيحة";
            return Page();
        }

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var home = roles.Contains(AppRoles.Admin) ? "/admin"
            : roles.Contains(AppRoles.Employee) ? "/employee/call"
            : roles.Contains(AppRoles.Kiosk) ? "/kiosk"
            : roles.Contains(AppRoles.Display) ? "/display"
            : "/";

        return LocalRedirect(home);
    }
}
