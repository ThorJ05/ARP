using BusinessEsbjerg.Holiday.Domain.Calendar;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class CompanyDayOffRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly DateOnly TripDay = new(2027, 5, 7); // a Friday, not a public holiday

    // holiday Monday 3 to Friday 7 May 2027
    private static HolidayRequest Holiday(RequestStatus status) => new()
    {
        Status = status,
        AccountType = AccountType.Weeks1To5,
        StartDate = new DateOnly(2027, 5, 3),
        EndDate = new DateOnly(2027, 5, 7)
    };

    private static IReadOnlyList<string> Add(DateOnly date, params HolidayRequest[] holidays) =>
        CompanyDayOffRules.CheckAdd(date, "Company trip", Today, [], holidays);

    [Fact]
    public void Future_day_can_be_added()
    {
        Assert.Empty(Add(TripDay));
    }

    [Theory]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Pending)]
    [InlineData(RequestStatus.CancellationRequested)]
    public void Blocked_by_holiday(RequestStatus status)
    {
        Assert.Contains(Add(TripDay, Holiday(status)), e => e.Contains("holiday covers that date"));
    }

    [Theory]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Cancelled)]
    public void Declined_holiday_doesnt_block(RequestStatus status)
    {
        Assert.Empty(Add(TripDay, Holiday(status)));
    }

    [Fact]
    public void Past_date_rejected()
    {
        Assert.Contains(Add(Today), e => e.Contains("after today"));
    }

    [Fact]
    public void Public_holiday_rejected()
    {
        Assert.Contains(Add(new DateOnly(2027, 12, 25)), e => e.Contains("public holiday"));
    }

    [Fact]
    public void Duplicate_date_rejected()
    {
        var existing = new CompanyDayOff { Date = TripDay, Name = "Trip" };

        var errors = CompanyDayOffRules.CheckAdd(TripDay, "Another", Today, [existing], []);

        Assert.Contains(errors, e => e.Contains("already a company day off"));
    }

    [Fact]
    public void Cant_remove_inside_holiday()
    {
        var day = new CompanyDayOff { Date = TripDay, Name = "Trip" };

        var errors = CompanyDayOffRules.CheckRemove(day, Today, [Holiday(RequestStatus.Approved)]);

        Assert.Contains(errors, e => e.Contains("holiday covers that date"));
    }

    [Fact]
    public void Future_day_can_be_removed()
    {
        var day = new CompanyDayOff { Date = TripDay, Name = "Trip" };

        Assert.Empty(CompanyDayOffRules.CheckRemove(day, Today, []));
    }

    [Fact]
    public void Cant_remove_missing_or_past()
    {
        Assert.NotEmpty(CompanyDayOffRules.CheckRemove(null, Today, []));
        Assert.NotEmpty(CompanyDayOffRules.CheckRemove(
            new CompanyDayOff { Date = Today, Name = "Old" }, Today, []));
    }
}
