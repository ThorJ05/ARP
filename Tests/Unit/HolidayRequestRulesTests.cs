using BusinessEsbjerg.Holiday.Domain.Calendar;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class HolidayRequestRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);
    private static readonly HashSet<DateOnly> PublicHolidays2027 = [..DanishPublicHolidays.In(2027)];

    // hired 1 Sep 2025, so there is a full year of holiday behind
    private static readonly Employee Anna = FullTimeEmployee(new DateOnly(2025, 9, 1));

    // hired 2 Nov 2026, so there is almost nothing earned yet
    private static readonly Employee Dan = FullTimeEmployee(new DateOnly(2026, 11, 2));

    // the rules for one employee, with no ledger entries saved yet (everything earned is worked out from the hire date)
    private static HolidayRequestRules RulesFor(Employee employee, params HolidayRequest[] otherRequests) =>
        new(employee, Accounts(employee), [], otherRequests, Rules(), PublicHolidays2027, Today);

    private static NewHolidayRequest Weeks1To5(DateOnly start, DateOnly end) =>
        new(AccountType.Weeks1To5, start, end);

    [Fact]
    public void Week_costs_37_hours()
    {
        // Arrange
        var rules = RulesFor(Anna);
        var request = Weeks1To5(new DateOnly(2027, 1, 11), new DateOnly(2027, 1, 15));

        // Act
        var check = rules.Check(request);

        // Assert
        Assert.True(check.IsValid);
        Assert.Equal(5, check.WorkingDays);
        Assert.Equal(37m, check.Hours);
    }

    [Fact]
    public void Holidays_are_free()
    {
        // Arrange: Easter 2027, Thursday 25, Friday 26 and Monday 29 March are public holidays
        var rules = RulesFor(Anna);
        var request = Weeks1To5(new DateOnly(2027, 3, 22), new DateOnly(2027, 3, 31));

        // Act
        var check = rules.Check(request);

        // Assert
        Assert.Equal(5, check.WorkingDays);
        Assert.Equal(37.5m, check.Hours);
    }

    [Fact]
    public void Needs_one_month_notice()
    {
        // Arrange
        var rules = RulesFor(Anna);
        var request = Weeks1To5(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 1));

        // Act
        var check = rules.Check(request);

        // Assert
        var error = Assert.Single(check.Errors);
        Assert.Contains("1 month ahead", error);
        Assert.Contains("2026-12-18", error);
    }

    [Fact]
    public void Main_holiday_in_summer()
    {
        // Arrange
        var rules = RulesFor(Anna);
        var request = Weeks1To5(new DateOnly(2027, 7, 5), new DateOnly(2027, 7, 23));

        // Act
        var check = rules.Check(request);

        // Assert
        Assert.True(check.IsValid);
        Assert.Equal(15, check.WorkingDays);
    }

    [Fact]
    public void Main_holiday_ends_by_30_Sep()
    {
        // Arrange
        var rules = RulesFor(Anna);
        var request = Weeks1To5(new DateOnly(2027, 9, 20), new DateOnly(2027, 10, 8));

        // Act
        var check = rules.Check(request);

        // Assert
        var error = Assert.Single(check.Errors);
        Assert.Contains("over by 2027-09-30", error);
    }

    [Fact]
    public void Max_5_days_debt()
    {
        // Arrange: about 30 hours earned, 74 hours requested
        var rules = RulesFor(Dan);
        var request = Weeks1To5(new DateOnly(2027, 1, 11), new DateOnly(2027, 1, 22));

        // Act
        var check = rules.Check(request);

        // Assert
        var error = Assert.Single(check.Errors);
        Assert.Contains("borrow at most 5 days", error);
    }

    [Fact]
    public void No_debt_on_31_Aug()
    {
        // Arrange: within the debt limit every day, but still 9.61 hours short on 31 August
        var rules = RulesFor(Dan);
        var request = Weeks1To5(new DateOnly(2027, 8, 2), new DateOnly(2027, 8, 31));

        // Act
        var check = rules.Check(request);

        // Assert
        var error = Assert.Single(check.Errors);
        Assert.Contains("-9.61 hours on 2027-08-31", error);
    }

    [Fact]
    public void Week6_usable_hours()
    {
        // Arrange: 4 months of 2025 = 12.32 hours, usable from 1 May 2026. One day costs 7.5 hours.
        var rules = RulesFor(Anna);
        var request = new NewHolidayRequest(AccountType.Week6, new DateOnly(2027, 1, 11), new DateOnly(2027, 1, 11));

        // Act
        var check = rules.Check(request);

        // Assert
        Assert.True(check.IsValid);
    }

    [Fact]
    public void Week6_too_many_hours()
    {
        // Arrange: two days cost 15 hours, but only 12.32 hours are usable
        var rules = RulesFor(Anna);
        var request = new NewHolidayRequest(AccountType.Week6, new DateOnly(2027, 1, 11), new DateOnly(2027, 1, 12));

        // Act
        var check = rules.Check(request);

        // Assert
        var error = Assert.Single(check.Errors);
        Assert.Contains("12.32 hours of week 6", error);
    }

    [Fact]
    public void Overlapping_request()
    {
        // Arrange
        var pending = new HolidayRequest
        {
            EmployeeId = Anna.Id,
            AccountType = AccountType.Weeks1To5,
            StartDate = new DateOnly(2027, 1, 11),
            EndDate = new DateOnly(2027, 1, 15)
        };
        var rules = RulesFor(Anna, pending);
        var request = Weeks1To5(new DateOnly(2027, 1, 14), new DateOnly(2027, 1, 20));

        // Act
        var check = rules.Check(request);

        // Assert
        Assert.Contains(check.Errors, e => e.Contains("already have a request"));
    }

    [Fact]
    public void Before_first_day()
    {
        // Arrange: Dan starts on 2 November 2026
        var rules = RulesFor(Dan);
        var request = Weeks1To5(new DateOnly(2026, 10, 26), new DateOnly(2026, 11, 6));

        // Act
        var check = rules.Check(request);

        // Assert
        var error = Assert.Single(check.Errors);
        Assert.Contains("before your first day", error);
    }

    [Fact]
    public void Weekend_only()
    {
        // Arrange: Saturday 16 and Sunday 17 January 2027
        var rules = RulesFor(Anna);
        var request = Weeks1To5(new DateOnly(2027, 1, 16), new DateOnly(2027, 1, 17));

        // Act
        var check = rules.Check(request);

        // Assert
        var error = Assert.Single(check.Errors);
        Assert.Contains("no working days", error);
    }
}
