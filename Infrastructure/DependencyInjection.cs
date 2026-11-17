using Infrastructure.Auth;
using Infrastructure.Repositories;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Service.Abstractions;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Lets Linq2db run queries through the EF Core model and connection
        LinqToDBForEFTools.Initialize();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        return services;
    }
}
