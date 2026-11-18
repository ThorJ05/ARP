using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class RequestDecisionTests
{
    private static readonly Guid Accountant = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid AccountId = Guid.NewGuid();

    private static HolidayRequest Request(RequestStatus status) => new()
    {
        Status = status,
        AccountType = AccountType.Weeks1To5,
        StartDate = new DateOnly(2027, 1, 11),
        EndDate = new DateOnly(2027, 1, 15),
        Hours = 37m
    };

    [Fact]
    public void Approving_records_who_and_when()
    {
        var request = Request(RequestStatus.Pending);

        Assert.Null(request.Approve(Accountant, null, Now));

        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.Equal(Accountant, request.DecidedByUserId);
        Assert.Equal(Now, request.DecidedAtUtc);
    }

    [Fact]
    public void Declining_needs_a_reason()
    {
        var request = Request(RequestStatus.Pending);

        Assert.Equal(Review.ReasonMissing, request.Decline(Accountant, " ", Now));
        Assert.Null(request.Decline(Accountant, "Too many people away that week", Now));

        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Equal("Too many people away that week", request.DecisionReason);
    }

    [Fact]
    public void An_approved_cancellation_cancels_and_a_declined_one_keeps_the_holiday()
    {
        var approved = Request(RequestStatus.CancellationRequested);
        var declined = Request(RequestStatus.CancellationRequested);

        approved.Approve(Accountant, null, Now);
        declined.Decline(Accountant, "We already booked a replacement", Now);

        Assert.Equal(RequestStatus.Cancelled, approved.Status);
        Assert.Equal(RequestStatus.Approved, declined.Status);
    }

    [Fact]
    public void A_decided_request_cant_be_decided_again()
    {
        var request = Request(RequestStatus.Rejected);

        Assert.NotNull(request.Approve(Accountant, null, Now));
        Assert.Equal(RequestStatus.Rejected, request.Status);
    }

    [Fact]
    public void Approved_holiday_uses_each_working_day()
    {
        var employee = FullTimeEmployee(new DateOnly(2025, 9, 1));
        var request = Request(RequestStatus.Approved);
        var days = HolidayDays.Between(employee, request.StartDate, request.EndDate, new HashSet<DateOnly>());

        var usage = RequestLedger.Usage(request, AccountId, days);

        Assert.Equal(5, usage.Count);
        Assert.Equal(-37m, usage.Sum(e => e.Hours));
        Assert.All(usage, e => Assert.Equal(request.Id, e.RequestId));
    }

    [Fact]
    public void A_cancellation_gives_back_exactly_the_hours_from_the_right_period()
    {
        var employee = FullTimeEmployee(new DateOnly(2025, 9, 1));
        var request = Request(RequestStatus.Approved);
        var old = Entry(20m, new DateOnly(2026, 8, 31), new DateOnly(2027, 12, 31));
        var usage = RequestLedger.Usage(request, AccountId,
            HolidayDays.Between(employee, request.StartDate, request.EndDate, new HashSet<DateOnly>()));
        var ledger = new List<LedgerEntry> { old };
        ledger.AddRange(usage);

        var before = BalanceCalculator.Calculate(AccountType.Weeks1To5, ledger, new DateOnly(2027, 2, 1));
        ledger.AddRange(RequestLedger.Cancellation(request, ledger));
        var after = BalanceCalculator.Calculate(AccountType.Weeks1To5, ledger, new DateOnly(2027, 2, 1));

        Assert.Equal(-17m, before.TotalHours);
        Assert.Equal(20m, after.TotalHours);
        Assert.Equal(new DateOnly(2027, 12, 31), Assert.Single(after.Buckets).ExpiresOn);
    }

    [Fact]
    public void A_transfer_moves_hours_to_the_next_period()
    {
        var transfer = new TransferRequest
        {
            AccountType = AccountType.Weeks1To5,
            FromPeriodStart = new DateOnly(2025, 9, 1),
            Hours = 37m
        };
        var earned = Entry(184.68m, new DateOnly(2026, 8, 31), new DateOnly(2026, 12, 31));
        var ledger = new List<LedgerEntry> { earned };
        ledger.AddRange(RequestLedger.Transfer(transfer, AccountId, new DateOnly(2026, 11, 18)));

        var nextYear = BalanceCalculator.Calculate(AccountType.Weeks1To5, ledger, new DateOnly(2027, 1, 5));

        Assert.Equal(37m, nextYear.TotalHours);         // the transferred week is still there
        Assert.Equal(147.68m, nextYear.ExpiredHours);   // the rest of the old period expired
    }
}
