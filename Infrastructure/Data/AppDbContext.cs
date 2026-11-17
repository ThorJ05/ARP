using BusinessEsbjerg.Holiday.Domain.Entities;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<WorkScheduleEntry> WorkScheduleEntries => Set<WorkScheduleEntry>();
    public DbSet<HolidayAccount> HolidayAccounts => Set<HolidayAccount>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<HolidayRequest> HolidayRequests => Set<HolidayRequest>();
    public DbSet<RuleParameter> RuleParameters => Set<RuleParameter>();
    public DbSet<CompanyDayOff> CompanyDaysOff => Set<CompanyDayOff>();
    public DbSet<TransferRequest> TransferRequests => Set<TransferRequest>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Linq2db reuses this context's connection and model, plus the extra mapping
        optionsBuilder.UseLinqToDB(builder => builder.AddMappingSchema(LinqToDbMapping.Schema));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}