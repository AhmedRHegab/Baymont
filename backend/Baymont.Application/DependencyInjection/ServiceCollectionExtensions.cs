using Baymont.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Baymont.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SearchService>();
        services.AddScoped<OwnerService>();
        services.AddScoped<UnitService>();
        services.AddScoped<VehicleService>();
        services.AddScoped<ContractorService>();
        services.AddScoped<DeliveryService>();
        services.AddScoped<SiteOperationsService>();
        services.AddScoped<ImportService>();
        services.AddScoped<ReservationService>();
        services.AddSingleton<InboxStatus>();
        services.AddScoped<DemoDataService>();
        services.AddScoped<AuditQueryService>();
        return services;
    }
}
