using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BusinessEsbjerg.Holiday.Infrastructure.Data;

/// <summary>
/// Inserts test data in the development environment. Safe to run multiple times
/// (each step is skipped if its table already has data).
/// All test users share the password "Test1234!" (development only).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Linq2db connection that reuses the EF Core connection and table mapping
        await using var db = context.CreateLinqToDBConnection();

        await SeedRuleParametersAsync(db);
        await SeedCompanyDaysOffAsync(db);
        await SeedEmployeesAsync(db);
        await SeedUsersAsync(db);
    }

    private static async Task SeedRuleParametersAsync(DataConnection db)
    {
        if (await db.GetTable<RuleParameter>().AnyAsync()) return;

        var start = new DateOnly(2020, 9, 1); // new Holiday Act in force
        RuleParameter P(string key, string value, string desc, DateOnly? from = null) =>
            new() { Key = key, Value = value, Description = desc, EffectiveFrom = from ?? start };

        var rules = new[]
        {
            P(RuleKeys.Weeks1To5DaysPerMonth, "2.08", "Weeks 1-5: holiday days earned per month"),
            P(RuleKeys.Week6Enabled, "true", "6th holiday week enabled"),
            P(RuleKeys.Week6HoursPerMonth, "3.083", "6th week: hours earned per month (full time)"),
            P(RuleKeys.Week6HoursPerCalendarDay, "0.103", "6th week: hours earned per calendar day in a partial month"),
            P(RuleKeys.FullTimeWeeklyHours, "37", "Full time: hours per week"),
            P(RuleKeys.MainHolidayDays, "15", "Main holiday: consecutive working days"),
            P(RuleKeys.MainHolidayWindowStart, "05-01", "Main holiday: window start (MM-dd)"),
            P(RuleKeys.MainHolidayWindowEnd, "09-30", "Main holiday: window end (MM-dd)"),
            P(RuleKeys.NoticeMainHolidayMonths, "3", "Notice for main holiday (months)"),
            P(RuleKeys.NoticeOtherHolidayMonths, "1", "Notice for other holiday (months)"),
            P(RuleKeys.SupplementBasePercent, "1.00", "Holiday supplement: Holiday Agreement (%)"),
            // Raised holiday supplement: 0.95% until 1 May 2024, then 1.48%
            P(RuleKeys.SupplementRaisedPercent, "0.95", "Holiday supplement: raised (%)"),
            P(RuleKeys.SupplementRaisedPercent, "1.48", "Holiday supplement: raised (%)", new DateOnly(2024, 5, 1)),
            P(RuleKeys.PayoutPercentWeeks1To5, "12.5", "Holiday pay / payout, weeks 1-5 (%)"),
            P(RuleKeys.PayoutPercentWeek6, "2.5", "Holiday pay / payout, week 6 (%)"),
            P(RuleKeys.TransferDeadlineWeek5, "12-31", "Transfer deadline, 5th week (MM-dd)"),
            P(RuleKeys.TransferDeadlineWeek6, "09-30", "Transfer deadline, week 6 (MM-dd)")
        };

        foreach (var rule in rules)
            await db.InsertAsync(rule);
    }

    private static async Task SeedCompanyDaysOffAsync(DataConnection db)
    {
        if (await db.GetTable<CompanyDayOff>().AnyAsync()) return;

        foreach (var year in new[] { 2026, 2027, 2028 })
        {
            await db.InsertAsync(new CompanyDayOff { Date = new DateOnly(year, 12, 24), Name = "Christmas Eve" });
            await db.InsertAsync(new CompanyDayOff { Date = new DateOnly(year, 12, 31), Name = "New Year's Eve" });
        }
    }

    private static async Task SeedEmployeesAsync(DataConnection db)
    {
        if (await db.GetTable<Employee>().AnyAsync()) return;

        // Full time
        var anna = NewEmployee("Anna Fulltime", new DateOnly(2023, 1, 1), PayType.Salaried);
        // Part time: Mon-Wed 7.5 h = 22.5 h/week
        var bo = NewEmployee("Bo Parttime", new DateOnly(2024, 3, 1), PayType.Salaried);
        // Hourly paid
        var carla = NewEmployee("Carla Hourly", new DateOnly(2025, 6, 1), PayType.Hourly);
        // New starter this month
        var dan = NewEmployee("Dan Newstarter", new DateOnly(2026, 11, 3), PayType.Salaried);
        // Has left the company
        var erik = NewEmployee("Erik Terminated", new DateOnly(2021, 2, 1), PayType.Salaried);
        erik.TerminationDate = new DateOnly(2026, 9, 30);

        // The database generates each Id, and Linq2db returns it
        foreach (var e in new[] { anna, bo, carla, dan, erik })
            e.Id = await db.InsertWithInt32IdentityAsync(e);

        foreach (var e in new[] { anna, carla, dan, erik })
            await db.InsertAsync(WorkScheduleEntry.FullTime(e.Id, e.HireDate));

        await db.InsertAsync(new WorkScheduleEntry
        {
            EmployeeId = bo.Id,
            ValidFrom = bo.HireDate,
            MonHours = 7.5m, TueHours = 7.5m, WedHours = 7.5m
        });

        foreach (var e in new[] { anna, bo, carla, dan, erik })
        {
            await db.InsertAsync(new HolidayAccount { EmployeeId = e.Id, Type = AccountType.Weeks1To5 });
            await db.InsertAsync(new HolidayAccount { EmployeeId = e.Id, Type = AccountType.Week6 });
        }

        // Salary history for Anna (holiday-qualifying salary per month) so the DKK calculation can be tested
        for (var month = new DateOnly(2025, 9, 1); month <= new DateOnly(2026, 10, 1); month = month.AddMonths(1))
        {
            await db.InsertAsync(new MonthlySalaryRecord
            {
                EmployeeId = anna.Id,
                Month = month,
                QualifyingSalary = 40000m,
                HoursWorked = 160.33m
            });
        }
    }

    private static async Task SeedUsersAsync(DataConnection db)
    {
        if (await db.GetTable<User>().AnyAsync()) return;

        var hasher = new PasswordHasher<User>();
        var anna = await db.GetTable<Employee>().FirstAsync(e => e.Name.StartsWith("Anna"));

        User NewUser(string email, Role role, int? employeeId)
        {
            var u = new User { Email = email, Role = role, EmployeeId = employeeId };
            u.PasswordHash = hasher.HashPassword(u, "Test1234!");
            return u;
        }

        var users = new[]
        {
            NewUser("anna@businessesbjerg.dk", Role.Employee, anna.Id),
            NewUser("regnskab@businessesbjerg.dk", Role.Accounting, null),
            NewUser("admin@businessesbjerg.dk", Role.Admin, null)
        };

        foreach (var user in users)
            await db.InsertAsync(user);
    }

    private static Employee NewEmployee(string name, DateOnly hired, PayType payType) =>
        new() { Name = name, HireDate = hired, PayType = payType };
}