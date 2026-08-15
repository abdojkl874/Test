using HospitalQueue.Application.Interfaces;
using HospitalQueue.Application.Services;
using HospitalQueue.Domain.Constants;
using HospitalQueue.Domain.Entities;
using HospitalQueue.Infrastructure.Data;
using HospitalQueue.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Serves static files bundled inside referenced packages (e.g. MudBlazor's
// wwwroot/MudBlazor.min.css/js under _content/MudBlazor/...). ASP.NET Core
// only wires this up automatically when it detects the Development
// environment; calling it explicitly makes package assets resolve
// regardless of how the environment ends up configured on a given machine.
builder.WebHost.UseStaticWebAssets();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Kept short so short employee/admin passwords aren't rejected outright;
        // the Kiosk/Display screens don't authenticate through Identity at all
        // (see Shared/ScreenPasswordGate.razor) so this only governs user accounts.
        options.Password.RequiredLength = 4;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
        options.User.RequireUniqueEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// QueueEventBus is the singleton pub/sub every circuit subscribes to for live
// updates (see its doc comment) — Blazor Server's own connection already
// provides the "real-time / WebSocket" transport, so no extra SignalR hub is needed.
builder.Services.AddSingleton<QueueEventBus>();
builder.Services.AddSingleton<IQueueNotifier>(sp => sp.GetRequiredService<QueueEventBus>());

builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();
