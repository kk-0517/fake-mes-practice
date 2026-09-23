using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FakeMes.HttpApi;

public static class FakeMesHttpApiModule
{
    public static IServiceCollection AddFakeMesHttpApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly());
        return services;
    }
}
