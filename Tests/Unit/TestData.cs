using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Rules;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

// Employees, schedules and rules used by the unit tests.
public static class TestData
{
    private static readonly DateOnly RulesStart = new(2020, 9, 1);

    // The same rule values as the seeder. Extra rows can be added to test a rule change.
    public static RuleSet Rules(params RuleParameter[] changes) => new(
    [
        Rule(RuleKeys.Weeks1To5DaysPerMonth, "2.08"),
        Rule(RuleKeys.Week6Enabled, "true"),
        Rule(RuleKeys.Week6HoursPerMonth, "3.083"),
        Rule(RuleKeys.Week6HoursPerCalendarDay, "0.103"),
        Rule(RuleKeys.FullTimeWeeklyHours, "37"),
        Rule(RuleKeys.MainHolidayDays, "15"),
        Rule(RuleKeys.MainHolidayWindowStart, "05-01"),
        Rule(RuleKeys.MainHolidayWindowEnd, "09-30"),
        Rule(RuleKeys.NoticeMainHolidayMonths, "3"),
        Rule(RuleKeys.NoticeOtherHolidayMonths, "1"),
        Rule(RuleKeys.HolidayDebtMaxDays, "5"),
        Rule(RuleKeys.HolidayDebtSettleDate, "08-31"),
        ..changes
    ]);

    public static RuleParameter Rule(string key, string value, DateOnly? from = null) =>
        new() { Key = key, Value = value, EffectiveFrom = from ?? RulesStart };

    // Full time from the hire date: Mon-Thu 7.5 h, Fri 7 h (37 h, 7.4 h a day on average).
    public static Employee FullTimeEmployee(DateOnly hired, DateOnly? terminated = null)
    {
        var employee = new Employee { Name = "Test", HireDate = hired, TerminationDate = terminated };
        employee.WorkSchedule.Add(WorkScheduleEntry.FullTime(employee.Id, hired));
        return employee;
    }

    // Part time Mon-Wed 7.5 h (22.5 h a week, 7.5 h a day on average).
    public static WorkScheduleEntry PartTime(Guid employeeId, DateOnly validFrom) => new()
    {
        EmployeeId = employeeId,
        ValidFrom = validFrom,
        MondayStart = new TimeOnly(8, 0), MondayEnd = new TimeOnly(15, 30),
        TuesdayStart = new TimeOnly(8, 0), TuesdayEnd = new TimeOnly(15, 30),
        WednesdayStart = new TimeOnly(8, 0), WednesdayEnd = new TimeOnly(15, 30)
    };

    public static List<HolidayAccount> Accounts(Employee employee) =>
    [
        new() { EmployeeId = employee.Id, Type = AccountType.Weeks1To5 },
        new() { EmployeeId = employee.Id, Type = AccountType.Week6 }
    ];

    public static LedgerEntry Entry(decimal hours, DateOnly date, DateOnly? expiresOn = null,
        LedgerEntryType type = LedgerEntryType.Accrual) =>
        new() { Hours = hours, Date = date, ExpiresOn = expiresOn, Type = type };
}
