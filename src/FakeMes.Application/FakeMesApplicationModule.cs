using FakeMes.Application.Contracts.Maintenance;
using FakeMes.Application.Contracts.Stations;
using FakeMes.Application.Maintenance;
using FakeMes.Application.Stations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FakeMes.Application;

public static class FakeMesApplicationModule
{
    public static IServiceCollection AddFakeMesApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MaintenanceOptions>(
            configuration.GetSection(MaintenanceOptions.SectionName));

        services.AddScoped<IStationAppService, StationAppService>();
        services.AddScoped<IMaintenanceAppService, MaintenanceAppService>();
        services.AddHostedService<MaintenanceBackgroundService>();

        return services;
    }
}
