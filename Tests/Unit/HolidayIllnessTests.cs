using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class HolidayIllnessTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Employee Anna = FullTimeEmployee(new DateOnly(2025, 9, 1));

    // two weeks of approved holiday, Monday 11 to Friday 22 January 2027
    private static readonly HolidayRequest Holiday = new()
    {
        Status = RequestStatus.Approved,
        AccountType = AccountType.Weeks1To5,
        StartDate = new DateOnly(2027, 1, 11),
        EndDate = new DateOnly(2027, 1, 22)
    };

    private static List<LedgerEntry> Usage() =>
        RequestLedger.Usage(Holiday, AccountId,
            HolidayDays.Between(Anna, Holiday.StartDate, Holiday.EndDate, new HashSet<DateOnly>())).ToList();

    private static HolidayIllness Illness(IReadOnlyList<LedgerEntry> entries, HolidayRequest? request = null) =>
        new(request ?? Holiday, entries, Rules(Rule(RuleKeys.IllnessWaitingDays, "5")));

    [Fact]
    public void Days_after_5_are_returned()
    {
        var days = Illness(Usage()).DaysToGiveBack(new DateOnly(2027, 1, 11), new DateOnly(2027, 1, 22));

        Assert.Equal(5, days.Count);
        Assert.Equal(new DateOnly(2027, 1, 18), days[0].Date);
        Assert.Equal(-37m, days.Sum(d => d.Hours)); // the usage entries, given back as +37 hours
    }

    [Fact]
    public void Three_days_are_waiting_days()
    {
        var errors = Illness(Usage()).Check(new DateOnly(2027, 1, 11), new DateOnly(2027, 1, 13));

        Assert.Contains("Nothing to give back", Assert.Single(errors));
    }

    [Fact]
    public void Not_returned_twice()
    {
        var entries = Usage();
        var illness = Illness(entries);
        var first = new DateOnly(2027, 1, 11);
        var last = new DateOnly(2027, 1, 22);
        entries.AddRange(RequestLedger.Illness(Holiday, illness.DaysToGiveBack(first, last)));

        Assert.Empty(Illness(entries).DaysToGiveBack(first, last));
    }

    [Fact]
    public void Must_be_within_holiday()
    {
        var errors = Illness(Usage()).Check(new DateOnly(2027, 1, 20), new DateOnly(2027, 1, 26));

        Assert.Contains(errors, e => e.Contains("within the holiday"));
    }

    [Fact]
    public void Only_on_approved_holiday()
    {
        var pending = new HolidayRequest
        {
            Status = RequestStatus.Pending, StartDate = Holiday.StartDate, EndDate = Holiday.EndDate
        };

        var errors = Illness([], pending).Check(Holiday.StartDate, Holiday.EndDate);

        Assert.Contains(errors, e => e.Contains("only be registered on approved holiday"));
    }
}
