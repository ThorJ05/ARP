using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Api.Setup;

public static class DatabaseSetup
{
    // Runs the migrations, adds the rule values (and test data in development) and creates the accountant
    // and system owner logins on a new database (from fly secrets in production).
    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        await services.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<DbSeeder>().SeedAsync(includeTestData: app.Environment.IsDevelopment());

        var warnings = await services.GetRequiredService<AccountBootstrapper>().EnsureAsync(
        [
            LoginFromConfig(app.Configuration, Role.Accountant),
            LoginFromConfig(app.Configuration, Role.SystemOwner)
        ]);
        foreach (var warning in warnings)
            app.Logger.LogWarning("{Warning}", warning);
    }

    // e.g. Bootstrap:SystemOwner:Email (Bootstrap__SystemOwner__Email as a fly secret)
    private static BootstrapLogin LoginFromConfig(IConfiguration config, Role role) =>
        new(role, config[$"Bootstrap:{role}:Email"], config[$"Bootstrap:{role}:Password"]);
}
