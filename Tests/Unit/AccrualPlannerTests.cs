using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class AccrualPlannerTests
{
    [Fact]
    public void One_accrual_per_account_per_month_up_to_the_date()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));
        var accounts = Accounts(employee);

        var entries = AccrualPlanner.Plan(employee, accounts, [], Rules(), new DateOnly(2026, 12, 31));

        Assert.Equal(24, entries.Count);
        Assert.All(entries, e => Assert.Equal(LedgerEntryType.Accrual, e.Type));
        Assert.Equal(184.68m, entries.Where(e => e.AccountId == accounts[0].Id).Sum(e => e.Hours)); // 12 * 15.39
    }

    [Fact]
    public void A_month_is_earned_on_its_last_day()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));

        var entries = AccrualPlanner.Plan(employee, Accounts(employee), [], Rules(), new DateOnly(2026, 2, 27));

        Assert.All(entries, e => Assert.Equal(new DateOnly(2026, 1, 31), e.Date)); // February isn't over yet
    }

    [Fact]
    public void Running_it_again_adds_nothing()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));
        var accounts = Accounts(employee);
        var until = new DateOnly(2026, 6, 30);
        var first = AccrualPlanner.Plan(employee, accounts, [], Rules(), until);

        var second = AccrualPlanner.Plan(employee, accounts, first, Rules(), until);

        Assert.Empty(second);
    }

    [Fact]
    public void A_new_schedule_keeps_the_number_of_days()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));
        employee.WorkSchedule.Add(PartTime(employee.Id, new DateOnly(2026, 7, 1)));
        var accounts = Accounts(employee);
        
        var weeks1To5 = accounts.Single(a => a.Type == AccountType.Weeks1To5);

        var entries = AccrualPlanner.Plan(employee, accounts, [], Rules(), new DateOnly(2026, 7, 1));
        var conversion = entries.Single(e => e.AccountId == weeks1To5.Id && e.Type == LedgerEntryType.ScheduleConversion);
        var balance = BalanceCalculator.Calculate(AccountType.Weeks1To5,
            entries.Where(e => e.AccountId == weeks1To5.Id), new DateOnly(2026, 7, 1));

        // 6 months * 15.39 = 92.34 h = 12.48 days at 7.4 h, which is 93.59 h at 7.5 h
        Assert.Equal(1.25m, conversion.Hours);
        Assert.Equal(93.59m, balance.TotalHours);
        Assert.Equal(Math.Round(92.34m / 7.4m, 2), Math.Round(balance.TotalHours / 7.5m, 2));
    }

    [Fact]
    public void Accrual_stops_at_the_end_date()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1), terminated: new DateOnly(2026, 3, 15));

        var entries = AccrualPlanner.Plan(employee, Accounts(employee), [], Rules(), new DateOnly(2026, 12, 31));

        Assert.Equal(new DateOnly(2026, 3, 15), entries.Max(e => e.Date));
        Assert.Equal(6, entries.Count);
    }

    [Fact]
    public void Accruals_get_the_expiry_date_of_their_period()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 8, 1));
        var accounts = Accounts(employee);

        var entries = AccrualPlanner.Plan(employee, accounts, [], Rules(), new DateOnly(2026, 9, 30));
        DateOnly? Expiry(AccountType type, int month) => entries
            .Single(e => e.AccountId == accounts.Single(a => a.Type == type).Id && e.PeriodMonth!.Value.Month == month)
            .ExpiresOn;

        Assert.Equal(new DateOnly(2026, 12, 31), Expiry(AccountType.Weeks1To5, 8));
        Assert.Equal(new DateOnly(2027, 12, 31), Expiry(AccountType.Weeks1To5, 9));
        Assert.Equal(new DateOnly(2028, 4, 30), Expiry(AccountType.Week6, 9));
    }
}
