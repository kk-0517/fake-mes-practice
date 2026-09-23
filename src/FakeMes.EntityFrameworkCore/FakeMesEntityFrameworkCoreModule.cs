using FakeMes.Domain.Repositories;
using FakeMes.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FakeMes.EntityFrameworkCore;

public static class FakeMesEntityFrameworkCoreModule
{
    public static IServiceCollection AddFakeMesEntityFrameworkCore(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<FakeMesDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IStationRepository, EfStationRepository>();
        services.AddScoped<ITrackRecordRepository, EfTrackRecordRepository>();
        services.AddScoped<IMaintenanceLogRepository, EfMaintenanceLogRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
