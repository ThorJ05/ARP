using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Data;

// Inserts the rule values (always) and test data (development only). Safe to run multiple times.
// All test users share the password "Test1234!" (development only).
public class DbSeeder(AppDbContext context, IPasswordHasher hasher)
{
    private const string EmailDomain = "@businessesbjerg.com";
    private const string DevPassword = "Test1234!";

    public async Task SeedAsync(bool includeTestData)
    {
        // Linq2db connection that reuses the EF Core connection and table mapping
        await using var db = context.CreateLinqToDBConnection();

        await SeedRuleParametersAsync(db);

        if (!includeTestData) return;
        await SeedCompanyDaysOffAsync(db);
        await SeedEmployeesAndUsersAsync(db);
    }

    // only adds rules that are missing, so new rules also get added to an existing database
    private static async Task SeedRuleParametersAsync(DataConnection db)
    {
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
            P(RuleKeys.TransferEnabled, "false", "Transfer of unused holiday allowed (Business Esbjerg does not allow it today)"),
            P(RuleKeys.TransferMaxDays, "5", "Transfer: maximum days per holiday period (written agreement required)"),
            P(RuleKeys.TransferDeadlineWeek5, "12-31", "Transfer deadline, 5th week (MM-dd)"),
            P(RuleKeys.TransferDeadlineWeek6, "09-30", "Transfer deadline, week 6 (MM-dd)"),
            P(RuleKeys.HolidayDebtMaxDays, "5", "Holiday debt: maximum days the balance may be negative"),
            P(RuleKeys.HolidayDebtSettleDate, "08-31", "Holiday debt: the balance must be 0 or positive on this date (MM-dd)"),
            P(RuleKeys.IllnessWaitingDays, "5", "Illness during holiday: the first sick holiday days that are not given back")
        };

        foreach (var rule in rules)
        {
            var exists = await db.GetTable<RuleParameter>()
                .AnyAsync(r => r.Key == rule.Key && r.EffectiveFrom == rule.EffectiveFrom);

            if (!exists)
                await db.InsertAsync(rule);
        }
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

    private async Task SeedEmployeesAndUsersAsync(DataConnection db)
    {
        if (await db.GetTable<Employee>().AnyAsync()) return;

        // Full time
        await AddEmployeeAsync(db, "Anna Fulltime", "anna", new DateOnly(2023, 1, 1));

        // Full time at first, then part time Mon-Wed from 1 Sep 2025 (to test schedule history)
        var bo = await AddEmployeeAsync(db, "Bo Parttime", "bo", new DateOnly(2024, 3, 1));
        await db.InsertAsync(new WorkScheduleEntry
        {
            EmployeeId = bo.Id,
            ValidFrom = new DateOnly(2025, 9, 1),
            MondayStart = new TimeOnly(8, 0), MondayEnd = new TimeOnly(15, 30),
            TuesdayStart = new TimeOnly(8, 0), TuesdayEnd = new TimeOnly(15, 30),
            WednesdayStart = new TimeOnly(8, 0), WednesdayEnd = new TimeOnly(15, 30)
        });

        // New starter this month
        await AddEmployeeAsync(db, "Dan Newstarter", "dan", new DateOnly(2026, 11, 3));

        // Has left the company
        await AddEmployeeAsync(db, "Erik Terminated", "erik", new DateOnly(2021, 2, 1),
            terminated: new DateOnly(2026, 9, 30));

        // accountant and system owner logins (not linked to an employee)
        await db.InsertAsync(NewUser("regnskab" + EmailDomain, Role.Accountant, null));
        await db.InsertAsync(NewUser("systemejer" + EmailDomain, Role.SystemOwner, null));
    }

    // creates an employee with a full-time schedule, both holiday accounts and a login
    private async Task<Employee> AddEmployeeAsync(DataConnection db, string name, string login, DateOnly hired,
        DateOnly? terminated = null)
    {
        var employee = new Employee { Name = name, HireDate = hired, TerminationDate = terminated };

        await db.InsertAsync(employee);
        await db.InsertAsync(WorkScheduleEntry.FullTime(employee.Id, hired));
        await db.InsertAsync(new HolidayAccount { EmployeeId = employee.Id, Type = AccountType.Weeks1To5 });
        await db.InsertAsync(new HolidayAccount { EmployeeId = employee.Id, Type = AccountType.Week6 });
        await db.InsertAsync(NewUser(login + EmailDomain, Role.Employee, employee.Id));

        return employee;
    }

    private User NewUser(string email, Role role, Guid? employeeId)
    {
        var user = new User { Email = email, Role = role, EmployeeId = employeeId };
        user.PasswordHash = hasher.Hash(user, DevPassword);
        return user;
    }
}
