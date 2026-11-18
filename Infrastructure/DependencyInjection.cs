using BusinessEsbjerg.Holiday.Infrastructure.Auth;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Infrastructure.Repositories;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessEsbjerg.Holiday.Infrastructure;

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
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<ILedgerRepository, LedgerRepository>();
        services.AddScoped<IHolidayRequestRepository, HolidayRequestRepository>();

        services.AddScoped<DbSeeder>();
        services.AddScoped<AccountBootstrapper>();
        return services;
    }
}
