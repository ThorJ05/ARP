using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BusinessEsbjerg.Holiday.Infrastructure.Data;

// is only used by dotnet ef to make imigrations. does not run in the app itself
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=holiday;Username=holiday;Password=holiday_dev_pw")
            .Options;

        return new AppDbContext(options);
    }
}