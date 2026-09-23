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
        return services;
    }
}
