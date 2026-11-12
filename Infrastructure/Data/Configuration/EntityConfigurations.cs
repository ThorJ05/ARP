using BusinessEsbjerg.Holiday.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessEsbjerg.Holiday.Infrastructure.Data.Configurations;



public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.PayType).HasConversion<string>().HasMaxLength(20);
        b.HasMany(x => x.WorkSchedule).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId);
        b.HasMany(x => x.Accounts).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId);
        b.HasMany(x => x.Requests).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId);
        b.HasMany(x => x.SalaryRecords).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId);
    }
}

public class WorkScheduleEntryConfiguration : IEntityTypeConfiguration<WorkScheduleEntry>
{
    public void Configure(EntityTypeBuilder<WorkScheduleEntry> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.WeeklyHours);
        foreach (var prop in new[]
                 {
                     nameof(WorkScheduleEntry.MonHours), nameof(WorkScheduleEntry.TueHours),
                     nameof(WorkScheduleEntry.WedHours), nameof(WorkScheduleEntry.ThuHours),
                     nameof(WorkScheduleEntry.FriHours), nameof(WorkScheduleEntry.SatHours),
                     nameof(WorkScheduleEntry.SunHours)
                 })
        {
            b.Property(prop).HasPrecision(8, 3);
        }

        // Én arbejdsplan pr. medarbejder pr. gyldig-fra-dato
        b.HasIndex(x => new { x.EmployeeId, x.ValidFrom }).IsUnique();
    }
}

public class HolidayAccountConfiguration : IEntityTypeConfiguration<HolidayAccount>
{
    public void Configure(EntityTypeBuilder<HolidayAccount> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.BalanceHours);
        b.HasMany(x => x.Entries).WithOne(x => x.Account).HasForeignKey(x => x.AccountId);

        // Præcis én konto af hver type pr. medarbejder
        b.HasIndex(x => new { x.EmployeeId, x.Type }).IsUnique();
    }
}

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Hours).HasPrecision(8, 3);
        b.Property(x => x.Reason).HasMaxLength(500);

        b.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.SetNull);

        // Idempotent optjening: samme konto + type + måned kan kun forekomme én gang.
        // NULL i PeriodMonth (forbrug, korrektion m.m.) regnes som forskellige og blokeres ikke.
        b.HasIndex(x => new { x.AccountId, x.Type, x.PeriodMonth }).IsUnique();
        b.HasIndex(x => new { x.AccountId, x.Date });
    }
}

public class HolidayRequestConfiguration : IEntityTypeConfiguration<HolidayRequest>
{
    public void Configure(EntityTypeBuilder<HolidayRequest> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Hours).HasPrecision(8, 3);
        b.Property(x => x.Comment).HasMaxLength(1000);
        b.Property(x => x.DecisionReason).HasMaxLength(1000);
        b.HasIndex(x => new { x.EmployeeId, x.StartDate, x.EndDate });
        b.HasIndex(x => x.Status);
    }
}

public class RuleParameterConfiguration : IEntityTypeConfiguration<RuleParameter>
{
    public void Configure(EntityTypeBuilder<RuleParameter> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(100).IsRequired();
        b.Property(x => x.Value).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);

        // Aldrig to værdier for samme nøgle med samme gyldig-fra-dato
        b.HasIndex(x => new { x.Key, x.EffectiveFrom }).IsUnique();
    }
}

public class CompanyDayOffConfiguration : IEntityTypeConfiguration<CompanyDayOff>
{
    public void Configure(EntityTypeBuilder<CompanyDayOff> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Date).IsUnique();
    }
}

public class MonthlySalaryRecordConfiguration : IEntityTypeConfiguration<MonthlySalaryRecord>
{
    public void Configure(EntityTypeBuilder<MonthlySalaryRecord> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.QualifyingSalary).HasPrecision(12, 2);
        b.Property(x => x.HoursWorked).HasPrecision(8, 3);
        b.HasIndex(x => new { x.EmployeeId, x.Month }).IsUnique();
    }
}