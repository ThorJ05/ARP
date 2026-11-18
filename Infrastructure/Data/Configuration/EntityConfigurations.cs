using BusinessEsbjerg.Holiday.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessEsbjerg.Holiday.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); // Guid is created in the entity, not by the database
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);

        // only one accountant and one system owner
        b.HasIndex(x => x.Role).IsUnique().HasFilter("\"Role\" <> 'Employee'");

        // one login per employee
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.EmployeeId).IsUnique();

        // employee logins must have an employee, accountant and system owner must not
        b.ToTable(t => t.HasCheckConstraint("CK_Users_EmployeeLink",
            "(\"Role\" = 'Employee') = (\"EmployeeId\" IS NOT NULL)"));
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); // Guid is created in the entity, not by the database
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasMany(x => x.WorkSchedule).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId);
        b.HasMany(x => x.Accounts).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId);
        b.HasMany(x => x.Requests).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId);
        b.HasMany(x => x.TransferRequests).WithOne(x => x.Employee).HasForeignKey(x => x.EmployeeId);
    }
}

public class WorkScheduleEntryConfiguration : IEntityTypeConfiguration<WorkScheduleEntry>
{
    private static readonly string[] WorkWeek = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];

    // a day needs both start and end (or none), and end must be after start
    private static readonly string ValidTimesSql = string.Join(" AND ", WorkWeek.Select(d =>
        $"(\"{d}Start\" IS NULL) = (\"{d}End\" IS NULL) AND (\"{d}Start\" IS NULL OR \"{d}End\" > \"{d}Start\")"));

    public void Configure(EntityTypeBuilder<WorkScheduleEntry> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); // Guid is created in the entity, not by the database
        b.Ignore(x => x.WeeklyHours);
        b.Ignore(x => x.WorkingDays);
        b.Ignore(x => x.AverageDayHours);

        // One work schedule per employee per valid-from date
        b.HasIndex(x => new { x.EmployeeId, x.ValidFrom }).IsUnique();

        b.ToTable(t => t.HasCheckConstraint("CK_WorkScheduleEntries_ValidTimes", ValidTimesSql));
    }
}

public class HolidayAccountConfiguration : IEntityTypeConfiguration<HolidayAccount>
{
    public void Configure(EntityTypeBuilder<HolidayAccount> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); // Guid is created in the entity, not by the database
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.HasMany(x => x.Entries).WithOne(x => x.Account).HasForeignKey(x => x.AccountId);

        // Exactly one account of each type per employee
        b.HasIndex(x => new { x.EmployeeId, x.Type }).IsUnique();
    }
}

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); // Guid is created in the entity, not by the database
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Hours).HasPrecision(8, 2);
        b.Property(x => x.Reason).HasMaxLength(500);

        b.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.TransferRequest).WithMany().HasForeignKey(x => x.TransferRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        // Idempotent accrual: the same account + type + month can only occur once.
        // NULL in PeriodMonth (usage, correction etc.) counts as distinct and is not blocked.
        b.HasIndex(x => new { x.AccountId, x.Type, x.PeriodMonth }).IsUnique();
        b.HasIndex(x => new { x.AccountId, x.Date });
    }
}

public class HolidayRequestConfiguration : IEntityTypeConfiguration<HolidayRequest>
{
    public void Configure(EntityTypeBuilder<HolidayRequest> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); // Guid is created in the entity, not by the database
        b.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Hours).HasPrecision(8, 2);
        b.Property(x => x.Comment).HasMaxLength(1000);
        b.Property(x => x.DecisionReason).HasMaxLength(1000);

        b.HasOne(x => x.DecidedBy).WithMany().HasForeignKey(x => x.DecidedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => new { x.EmployeeId, x.StartDate, x.EndDate });
        b.HasIndex(x => x.Status);

        b.ToTable(t => t.HasCheckConstraint("CK_HolidayRequests_Valid",
            "\"EndDate\" >= \"StartDate\" AND \"Hours\" > 0"));
    }
}

public class TransferRequestConfiguration : IEntityTypeConfiguration<TransferRequest>
{
    public void Configure(EntityTypeBuilder<TransferRequest> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); // Guid is created in the entity, not by the database
        b.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Hours).HasPrecision(8, 2);
        b.Property(x => x.Comment).HasMaxLength(1000);
        b.Property(x => x.DecisionReason).HasMaxLength(1000);

        b.HasOne(x => x.DecidedBy).WithMany().HasForeignKey(x => x.DecidedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => new { x.EmployeeId, x.FromPeriodStart });
        b.HasIndex(x => x.Status);

        b.ToTable(t => t.HasCheckConstraint("CK_TransferRequests_PositiveHours", "\"Hours\" > 0"));
    }
}

public class RuleParameterConfiguration : IEntityTypeConfiguration<RuleParameter>
{
    public void Configure(EntityTypeBuilder<RuleParameter> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); // Guid is created in the entity, not by the database
        b.Property(x => x.Key).HasMaxLength(100).IsRequired();
        b.Property(x => x.Value).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);

        // Never two values for the same key with the same effective-from date
        b.HasIndex(x => new { x.Key, x.EffectiveFrom }).IsUnique();
    }
}

public class CompanyDayOffConfiguration : IEntityTypeConfiguration<CompanyDayOff>
{
    public void Configure(EntityTypeBuilder<CompanyDayOff> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); // Guid is created in the entity, not by the database
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Date).IsUnique();
    }
}
