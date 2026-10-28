using Baymont.Application.Interfaces;
using Baymont.Domain.Entities;
using Baymont.Infrastructure.Identity;
using Baymont.Infrastructure.Interceptors;
using Baymont.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baymont.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<ValidationSaveChangesInterceptor>();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlite(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            options.AddInterceptors(
                sp.GetRequiredService<ValidationSaveChangesInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.Configure<Mail.MailOptions>(configuration.GetSection("Mail"));
        services.AddHostedService<Mail.InboxPollingService>();

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services
            .AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = false;
            })
            .AddErrorDescriber<ArabicIdentityErrorDescriber>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "Baymont.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        return services;
    }
}
