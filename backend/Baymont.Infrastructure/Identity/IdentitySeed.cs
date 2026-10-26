using Baymont.Domain.Constants;
using Baymont.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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

        // Public deployments must not ship with well-known passwords: outside Development the
        // admin password must come from configuration, and the gate user is only seeded when asked.
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var isDev = scope.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment();
        var adminPassword = config["ADMIN_INITIAL_PASSWORD"] ?? (isDev ? "ChangeMe!123" : null);
        var gatePassword = config["GATE_INITIAL_PASSWORD"] ?? (isDev ? "GateSearch!123" : null);
        var adminFromConfig = !string.IsNullOrEmpty(config["ADMIN_INITIAL_PASSWORD"]);

        const string adminUserName = "admin";
        var admin = await userManager.FindByNameAsync(adminUserName);
        if (admin is null)
        {
            if (adminPassword is null)
                throw new InvalidOperationException(
                    "No admin user exists. Set the ADMIN_INITIAL_PASSWORD environment variable for the first start.");

            admin = new AppUser
            {
                UserName = adminUserName,
                Email = "admin@baymont.local",
                DisplayName = "مدير النظام",
                IsActive = true,
                MustChangePassword = !adminFromConfig,
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
