using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Enums;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class BalanceCalculatorTests
{
    private static readonly DateOnly Expires2026 = new(2026, 12, 31);
    private static readonly DateOnly Expires2027 = new(2027, 12, 31);

    [Fact]
    public void Used_hours_come_from_the_holiday_that_expires_first()
    {
        var entries = new[]
        {
            Entry(20m, new DateOnly(2026, 8, 31), Expires2026),
            Entry(10m, new DateOnly(2026, 9, 30), Expires2027),
            Entry(-25m, new DateOnly(2026, 10, 5), type: LedgerEntryType.Usage)
        };

        var balance = BalanceCalculator.Calculate(AccountType.Weeks1To5, entries, new DateOnly(2026, 10, 5));

        var bucket = Assert.Single(balance.Buckets);
        Assert.Equal(Expires2027, bucket.ExpiresOn);
        Assert.Equal(5m, bucket.Hours);
        Assert.Equal(5m, balance.TotalHours);
    }

    [Fact]
    public void Hours_that_are_not_used_in_time_are_flagged_as_expired()
    {
        var entries = new[] { Entry(10m, new DateOnly(2026, 8, 31), Expires2026) };

        var balance = BalanceCalculator.Calculate(AccountType.Weeks1To5, entries, new DateOnly(2027, 1, 5));

        Assert.Equal(0m, balance.TotalHours);
        Assert.Equal(10m, balance.ExpiredHours);
    }

    [Fact]
    public void Week6_hours_can_only_be_used_from_1_May_the_year_after()
    {
        var entries = new[] { Entry(3.08m, new DateOnly(2026, 1, 31), new DateOnly(2028, 4, 30)) };

        var summer = BalanceCalculator.Calculate(AccountType.Week6, entries, new DateOnly(2026, 6, 1));
        var nextMay = BalanceCalculator.Calculate(AccountType.Week6, entries, new DateOnly(2027, 5, 1));

        Assert.Equal(3.08m, summer.TotalHours);
        Assert.Equal(0m, summer.AvailableHours);
        Assert.Equal(3.08m, nextMay.AvailableHours);
    }

    [Fact]
    public void Using_more_than_the_balance_gives_holiday_debt()
    {
        var entries = new[]
        {
            Entry(10m, new DateOnly(2026, 9, 30), Expires2027),
            Entry(-15m, new DateOnly(2026, 10, 5), type: LedgerEntryType.Usage)
        };

        var balance = BalanceCalculator.Calculate(AccountType.Weeks1To5, entries, new DateOnly(2026, 10, 5));

        Assert.Equal(-5m, balance.TotalHours);
    }

    [Fact]
    public void A_schedule_conversion_changes_every_unexpired_balance_by_the_same_factor()
    {
        var entries = new[]
        {
            Entry(10m, new DateOnly(2026, 8, 31), Expires2026),
            Entry(30m, new DateOnly(2026, 9, 30), Expires2027),
            Entry(4m, new DateOnly(2026, 10, 1), type: LedgerEntryType.ScheduleConversion)
        };

        var balance = BalanceCalculator.Calculate(AccountType.Weeks1To5, entries, new DateOnly(2026, 10, 1));

        Assert.Equal(new[] { 11m, 33m }, balance.Buckets.Select(b => b.Hours));
    }

    [Fact]
    public void Entries_after_the_date_are_not_counted()
    {
        var entries = new[]
        {
            Entry(15.39m, new DateOnly(2026, 9, 30), Expires2027),
            Entry(15.39m, new DateOnly(2026, 10, 31), Expires2027)
        };

        var balance = BalanceCalculator.Calculate(AccountType.Weeks1To5, entries, new DateOnly(2026, 10, 15));

        Assert.Equal(15.39m, balance.TotalHours);
    }
}
