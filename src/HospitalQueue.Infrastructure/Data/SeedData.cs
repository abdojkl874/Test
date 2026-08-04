using HospitalQueue.Domain.Constants;
using HospitalQueue.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HospitalQueue.Infrastructure.Data;

/// <summary>
/// Seeds the four fixed roles with their default permission claims, one
/// admin account to log in with the first time, and a small set of sample
/// clinics/counters so the screens aren't empty on first run.
/// Everything here is idempotent — safe to run on every startup.
/// </summary>
public static class SeedData
{
    private static readonly Dictionary<string, string[]> DefaultRolePermissions = new()
    {
        [AppRoles.Admin] = AppPermissions.All,
        [AppRoles.Employee] = new[] { AppPermissions.TicketsCall },
        [AppRoles.Kiosk] = new[] { AppPermissions.TicketsCreate },
        [AppRoles.Display] = new[] { AppPermissions.DisplayView },
    };

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        // No EF Core migrations are checked in yet (see README) — EnsureCreated
        // stands the schema up directly from the current model so `dotnet run`
        // works out of the box. Once real migrations are generated, switch this
        // to db.Database.MigrateAsync() so schema changes apply incrementally
        // instead of only working against an empty database.
        await db.Database.EnsureCreatedAsync();

        foreach (var roleName in AppRoles.All)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new ApplicationRole(roleName);
                await roleManager.CreateAsync(role);
            }

            var existingClaims = (await roleManager.GetClaimsAsync(role)).Select(c => c.Value).ToHashSet();
            foreach (var permission in DefaultRolePermissions[roleName])
            {
                if (!existingClaims.Contains(permission))
                {
                    await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(AppPermissions.ClaimType, permission));
                }
            }
        }

        var adminUsername = config["Seed:AdminUsername"] ?? "admin";
        var adminPassword = config["Seed:AdminPassword"] ?? "ChangeMe!2026";

        if (await userManager.FindByNameAsync(adminUsername) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminUsername,
                Email = config["Seed:AdminEmail"] ?? "admin@hospital.local",
                EmailConfirmed = true,
                FullName = "مدير النظام",
                IsActive = true,
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                logger.LogWarning(
                    "Seeded default admin account '{User}'. Change its password immediately after first login.",
                    adminUsername);
            }
            else
            {
                logger.LogError(
                    "Failed to seed default admin account: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await db.Services.AnyAsync())
        {
            var services1 = new[]
            {
                new Service { Id = Guid.NewGuid(), NameAr = "الباطنية", Code = "A", DisplayOrder = 1 },
                new Service { Id = Guid.NewGuid(), NameAr = "الأطفال", Code = "B", DisplayOrder = 2 },
                new Service { Id = Guid.NewGuid(), NameAr = "المختبر", Code = "L", DisplayOrder = 3 },
            };
            db.Services.AddRange(services1);

            var counters = new[]
            {
                new Counter { Id = Guid.NewGuid(), Name = "شباك 1" },
                new Counter { Id = Guid.NewGuid(), Name = "شباك 2" },
                new Counter { Id = Guid.NewGuid(), Name = "شباك 3" },
            };
            db.Counters.AddRange(counters);

            for (var i = 0; i < 3; i++)
            {
                db.CounterServices.Add(new CounterService { CounterId = counters[i].Id, ServiceId = services1[i].Id });
            }

            await db.SaveChangesAsync();
        }

        if (!await db.OrganizationSettings.AnyAsync())
        {
            db.OrganizationSettings.Add(new OrganizationSettings
            {
                Id = 1,
                OrgNameAr = "نظام إدارة طابور المشفى",
                TickerEnabled = false,
                TickerText = "مرحباً بكم — يرجى الانتظار حتى يتم مناداة رقمكم",
            });
            await db.SaveChangesAsync();
        }
    }
}
