using BusinessEsbjerg.Holiday.Domain.Calendar;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class HolidayRequestRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    // hired 1 Sep 2025, so there is a full year of holiday behind
    private static readonly Employee Anna = FullTimeEmployee(new DateOnly(2025, 9, 1));

    // hired 2 Nov 2026, so there is almost nothing earned yet
    private static readonly Employee Dan = FullTimeEmployee(new DateOnly(2026, 11, 2));

    private static RequestCheck Check(Employee employee, AccountType account, string start, string end,
        IReadOnlyList<HolidayRequest>? otherRequests = null)
    {
        var daysOff = new HashSet<DateOnly>(DanishPublicHolidays.In(2027));
        var rules = new HolidayRequestRules(
            employee, Accounts(employee), [], otherRequests ?? [], Rules(), daysOff, Today);
        return rules.Check(new NewHolidayRequest(account, DateOnly.Parse(start), DateOnly.Parse(end)));
    }

    [Fact]
    public void A_normal_week_costs_37_hours()
    {
        var check = Check(Anna, AccountType.Weeks1To5, "2027-01-11", "2027-01-15");

        Assert.True(check.IsValid);
        Assert.Equal(5, check.WorkingDays);
        Assert.Equal(37m, check.Hours);
    }

    [Fact]
    public void Weekends_and_public_holidays_are_free()
    {
        // Easter 2027: Thursday 25, Friday 26 and Monday 29 March are public holidays
        var check = Check(Anna, AccountType.Weeks1To5, "2027-03-22", "2027-03-31");

        Assert.Equal(5, check.WorkingDays);
        Assert.Equal(37.5m, check.Hours);
    }

    [Fact]
    public void Holiday_needs_one_month_notice()
    {
        var check = Check(Anna, AccountType.Weeks1To5, "2026-12-01", "2026-12-01");

        var error = Assert.Single(check.Errors);
        Assert.Contains("1 month ahead", error);
        Assert.Contains("2026-12-18", error);
    }

    [Fact]
    public void A_main_holiday_of_three_weeks_in_the_summer_is_fine()
    {
        var check = Check(Anna, AccountType.Weeks1To5, "2027-07-05", "2027-07-23");

        Assert.True(check.IsValid);
        Assert.Equal(15, check.WorkingDays);
    }

    [Fact]
    public void A_main_holiday_has_to_end_by_30_September()
    {
        var check = Check(Anna, AccountType.Weeks1To5, "2027-09-20", "2027-10-08");

        var error = Assert.Single(check.Errors);
        Assert.Contains("over by 2027-09-30", error);
    }

    [Fact]
    public void More_than_5_days_of_holiday_debt_is_not_allowed()
    {
        // about 30 hours earned, 74 hours requested
        var check = Check(Dan, AccountType.Weeks1To5, "2027-01-11", "2027-01-22");

        var error = Assert.Single(check.Errors);
        Assert.Contains("borrow at most 5 days", error);
    }

    [Fact]
    public void The_balance_must_not_be_negative_on_31_August()
    {
        // within the debt limit every day, but still 9.61 hours short on 31 August
        var check = Check(Dan, AccountType.Weeks1To5, "2027-08-02", "2027-08-31");

        var error = Assert.Single(check.Errors);
        Assert.Contains("-9.61 hours on 2027-08-31", error);
    }

    [Fact]
    public void Week6_can_only_use_hours_that_are_already_usable()
    {
        // 4 months of 2025 = 12.32 hours, usable from 1 May 2026
        var oneDay = Check(Anna, AccountType.Week6, "2027-01-11", "2027-01-11");
        var twoDays = Check(Anna, AccountType.Week6, "2027-01-11", "2027-01-12");

        Assert.True(oneDay.IsValid);
        Assert.Contains("12.32 hours of week 6", Assert.Single(twoDays.Errors));
    }

    [Fact]
    public void Days_that_are_already_requested_cant_be_requested_again()
    {
        var pending = new HolidayRequest
        {
            EmployeeId = Anna.Id,
            AccountType = AccountType.Weeks1To5,
            StartDate = new DateOnly(2027, 1, 11),
            EndDate = new DateOnly(2027, 1, 15)
        };

        var check = Check(Anna, AccountType.Weeks1To5, "2027-01-14", "2027-01-20", [pending]);

        Assert.Contains(check.Errors, e => e.Contains("already have a request"));
    }

    [Fact]
    public void Holiday_before_the_first_day_is_not_allowed()
    {
        var check = Check(Dan, AccountType.Weeks1To5, "2026-10-26", "2026-11-06");

        Assert.Contains("before your first day", Assert.Single(check.Errors));
    }

    [Fact]
    public void A_weekend_only_request_has_no_working_days()
    {
        var check = Check(Anna, AccountType.Weeks1To5, "2027-01-16", "2027-01-17");

        Assert.Contains("no working days", Assert.Single(check.Errors));
    }
}
