using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class TransferRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    // hired 1 Sep 2025: the period 2025/26 has 184.68 hours that expire on 31 Dec 2026
    private static readonly Employee Anna = FullTimeEmployee(new DateOnly(2025, 9, 1));

    private static IReadOnlyList<string> Check(string periodDate, decimal hours,
        IReadOnlyList<TransferRequest>? others = null, bool enabled = true, DateOnly? today = null)
    {
        var day = today ?? Today;
        var rules = Rules(
            Rule(RuleKeys.TransferEnabled, enabled ? "true" : "false"),
            Rule(RuleKeys.TransferMaxDays, "5"),
            Rule(RuleKeys.TransferDeadlineWeek5, "12-31"),
            Rule(RuleKeys.TransferDeadlineWeek6, "09-30"));
        var accounts = Accounts(Anna);
        var entries = AccrualPlanner.Plan(Anna, accounts, [], rules, day);
        var transferRules = new TransferRules(Anna, accounts, entries, others ?? [], rules, day);

        return transferRules.Check(new NewTransferRequest(AccountType.Weeks1To5, DateOnly.Parse(periodDate), hours));
    }

    private static TransferRequest Earlier(string periodDate, decimal hours, RequestStatus status) => new()
    {
        AccountType = AccountType.Weeks1To5,
        FromPeriodStart = DateOnly.Parse(periodDate),
        Hours = hours,
        Status = status
    };

    [Fact]
    public void Five_days_can_be_transferred()
    {
        Assert.Empty(Check("2025-09-01", 37m));
    }

    [Fact]
    public void Nothing_can_be_transferred_when_transfers_are_switched_off()
    {
        Assert.Contains("not allowed", Assert.Single(Check("2025-09-01", 7.4m, enabled: false)));
    }

    [Fact]
    public void More_than_5_days_is_not_allowed()
    {
        Assert.Contains("at most 5 days", Assert.Single(Check("2025-09-01", 37.5m)));
    }

    [Fact]
    public void The_5_days_are_for_the_whole_period_not_per_request()
    {
        var approved = Earlier("2025-09-01", 37m, RequestStatus.Approved);

        var errors = Check("2025-09-01", 7.4m, [approved]);

        Assert.Contains("37.00 hours from this period are already", Assert.Single(errors));
    }

    [Fact]
    public void Another_date_in_the_same_period_is_still_a_duplicate()
    {
        var pending = Earlier("2025-10-15", 7.4m, RequestStatus.Pending);

        var errors = Check("2025-09-01", 7.4m, [pending]);

        Assert.Contains("already have a transfer request waiting", Assert.Single(errors));
    }

    [Fact]
    public void Only_the_hours_left_in_the_period_can_be_transferred()
    {
        // only September and October 2026 are earned in the period 2026/27
        Assert.Contains("only 30.78 hours left", Assert.Single(Check("2026-09-01", 37m)));
    }

    [Fact]
    public void The_deadline_is_31_December_for_weeks_1_to_5()
    {
        var errors = Check("2025-09-01", 7.4m, today: new DateOnly(2027, 1, 5));

        Assert.Contains(errors, e => e.Contains("deadline") && e.Contains("2026-12-31"));
    }
}
