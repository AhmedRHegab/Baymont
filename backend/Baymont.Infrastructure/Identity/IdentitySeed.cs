using Baymont.Domain.Constants;
using Baymont.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baymont.Infrastructure.Identity;

public static class IdentitySeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeed");

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Passwords come from configuration (appsettings.json "Seed" section). On the server they can be
        // overridden with the environment variables Seed__AdminPassword / Seed__GatePassword.
        // They are only used when the user does not exist yet.
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var adminPassword = config["Seed:AdminPassword"];
        var gatePassword = config["Seed:GatePassword"];

        const string adminUserName = "admin";
        var admin = await userManager.FindByNameAsync(adminUserName);
        if (admin is null)
        {
            if (adminPassword is null)
                throw new InvalidOperationException(
                    "No admin user exists. Set Seed:AdminPassword in appsettings.json (or the Seed__AdminPassword environment variable).");

            admin = new AppUser
            {
                UserName = adminUserName,
                Email = "admin@baymont.local",
                DisplayName = "مدير النظام",
                IsActive = true,
                MustChangePassword = false,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create admin: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            logger.LogInformation("Seeded admin user 'admin'.");
        }

        const string gateUserName = "gate";
        var gate = await userManager.FindByNameAsync(gateUserName);
        if (gate is null && gatePassword is not null)
        {
            gate = new AppUser
            {
                UserName = gateUserName,
                Email = "gate@baymont.local",
                DisplayName = "أمن البوابة",
                IsActive = true,
                MustChangePassword = false,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(gate, gatePassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(gate, AppRoles.SearchOnly);
        }
    }
}
