using Microsoft.Extensions.DependencyInjection;
using Service.Abstractions;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}