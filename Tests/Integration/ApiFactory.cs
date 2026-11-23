using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace BusinessEsbjerg.Holiday.Tests.Integration;

// Starts the whole API in memory against a real PostgreSQL in Docker (Testcontainers), so the tests go
// through routing, login, validation, services, linq2db and the database, just like the real app.
// The database is new for every test run and the API migrates and seeds it on start, so the test users
// (anna, bo, regnskab ...) exist. Docker must be running.
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // an environment variable wins over appsettings and user secrets, so the API can't use another database
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _database.GetConnectionString());
    }

    // Development gives the seeded test users and the development JWT key
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", null);
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }
}

// All integration test classes share one API and one database, so it only starts once.
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
