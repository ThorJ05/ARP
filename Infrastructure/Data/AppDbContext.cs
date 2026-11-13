using BusinessEsbjerg.Holiday.Domain.Entities;
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
    public DbSet<MonthlySalaryRecord> MonthlySalaryRecords => Set<MonthlySalaryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
       //gets all IEntitityTypeConfig<T> in the project
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}