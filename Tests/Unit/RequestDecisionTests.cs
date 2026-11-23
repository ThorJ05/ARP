using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

// Tests for what happens when the accountant decides on a request, and for the ledger entries that follow.
public class RequestDecisionTests
{
    private static readonly Guid Accountant = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

    private static readonly IReadOnlySet<DateOnly> NoDaysOff = new HashSet<DateOnly>();

    private const decimal FullWeekHours = 37m;

    private static HolidayRequest Request(RequestStatus status) => new()
    {
        Status = status,
        AccountType = AccountType.Weeks1To5,
        StartDate = new DateOnly(2027, 1, 11),
        EndDate = new DateOnly(2027, 1, 15),
        Hours = FullWeekHours
    };

    private static IReadOnlyList<HolidayDay> WorkingDaysOf(HolidayRequest request)
    {
        var employee = FullTimeEmployee(new DateOnly(2025, 9, 1));
        return HolidayDays.Between(employee, request.StartDate, request.EndDate, NoDaysOff);
    }

    [Fact]
    public void Approving_a_pending_request_records_who_and_when()
    {
        // Arrange: a request that is waiting for a decision
        var request = Request(RequestStatus.Pending);

        // Act: the accountant approves it
        var error = request.Approve(Accountant, null, Now);

        // Assert: no error, the request is approved, and we know who decided and when
        Assert.Null(error);
        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.Equal(Accountant, request.DecidedByUserId);
        Assert.Equal(Now, request.DecidedAtUtc);
    }

    [Fact]
    public void Declining_without_a_reason_is_refused()
    {
        // Arrange: a pending request
        var request = Request(RequestStatus.Pending);

        // Act: the accountant declines, but the reason is only a space
        var error = request.Decline(Accountant, " ", Now);

        // Assert: we get the "reason missing" error and the request is unchanged
        Assert.Equal(Review.ReasonMissing, error);
        Assert.Equal(RequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Declining_with_a_reason_rejects_the_request()
    {
        // Arrange: a pending request and a reason
        var request = Request(RequestStatus.Pending);
        const string reason = "Too many people away that week";

        // Act: the accountant declines with the reason
        var error = request.Decline(Accountant, reason, Now);

        // Assert: no error, the request is rejected, and the reason is saved
        Assert.Null(error);
        Assert.Equal(RequestStatus.Rejected, request.Status);
        Assert.Equal(reason, request.DecisionReason);
    }

    [Fact]
    public void Approving_a_cancellation_request_cancels_the_holiday()
    {
        // Arrange: approved holiday that the employee has asked to cancel
        var request = Request(RequestStatus.CancellationRequested);

        // Act: the accountant approves the cancellation
        request.Approve(Accountant, null, Now);

        // Assert: the holiday is cancelled
        Assert.Equal(RequestStatus.Cancelled, request.Status);
    }

    [Fact]
    public void Declining_a_cancellation_request_keeps_the_holiday_approved()
    {
        // Arrange: approved holiday that the employee has asked to cancel
        var request = Request(RequestStatus.CancellationRequested);

        // Act: the accountant declines the cancellation (a reason is required)
        request.Decline(Accountant, "We already booked a replacement", Now);

        // Assert: the holiday stays approved
        Assert.Equal(RequestStatus.Approved, request.Status);
    }

    [Fact]
    public void A_decided_request_cant_be_decided_again()
    {
        // Arrange: a request that has already been rejected
        var request = Request(RequestStatus.Rejected);

        // Act: the accountant tries to approve it anyway
        var error = request.Approve(Accountant, null, Now);

        // Assert: we get an error and the status is unchanged
        Assert.NotNull(error);
        Assert.Equal(RequestStatus.Rejected, request.Status);
    }
    
    [Fact]
    public void Approved_holiday_uses_each_working_day()
    {
        // Arrange: an approved week and the working days in it
        var request = Request(RequestStatus.Approved);
        var days = WorkingDaysOf(request);

        // Act: create the ledger entries for the holiday
        var usage = RequestLedger.Usage(request, AccountId, days);

        // Assert: one negative entry per working day, 37 hours in total, all linked to the request
        Assert.Equal(5, usage.Count);
        Assert.Equal(-FullWeekHours, usage.Sum(e => e.Hours));
        Assert.All(usage, e => Assert.Equal(request.Id, e.RequestId));
    }

    [Fact]
    public void A_cancellation_gives_back_exactly_the_hours_from_the_right_period()
    {
        // Arrange: 20 earned hours (expiring 31 Dec 2027) and an approved week that used 37 hours.
        // The employee is now 17 hours in debt (20 - 37), which is allowed for up to a week.
        const decimal earnedHours = 20m;
        var balanceDate = new DateOnly(2027, 2, 1);
        var expiresOn = new DateOnly(2027, 12, 31);

        var request = Request(RequestStatus.Approved);
        var ledger = new List<LedgerEntry> { Entry(earnedHours, new DateOnly(2026, 8, 31), expiresOn) };
        ledger.AddRange(RequestLedger.Usage(request, AccountId, WorkingDaysOf(request)));

        var balanceBefore = BalanceCalculator.Calculate(AccountType.Weeks1To5, ledger, balanceDate);

        // Act: the cancellation is approved, so the hours are given back, and we look at the balance again
        var cancellation = RequestLedger.Cancellation(request, ledger).ToList();
        ledger.AddRange(cancellation);
        var balanceAfter = BalanceCalculator.Calculate(AccountType.Weeks1To5, ledger, balanceDate);

        // Assert: we started 17 hours in debt, and ended with the 20 earned hours again,
        // in the same period (the same expiry date) the hours came from
        Assert.Equal(earnedHours - FullWeekHours, balanceBefore.TotalHours);
        Assert.Equal(earnedHours, balanceAfter.TotalHours);
        Assert.Equal(expiresOn, Assert.Single(balanceAfter.Buckets).ExpiresOn);
    }

    [Fact]
    public void A_transfer_moves_hours_to_the_next_period()
    {
        // Arrange: 184.68 hours earned in the old period (expiring 31 Dec 2026),
        // and an approved transfer of one week (37 hours) to the next period
        const decimal earnedHours = 184.68m;
        const decimal transferredHours = FullWeekHours;

        var transfer = new TransferRequest
        {
            AccountType = AccountType.Weeks1To5,
            FromPeriodStart = new DateOnly(2025, 9, 1),
            Hours = transferredHours
        };
        var ledger = new List<LedgerEntry>
        {
            Entry(earnedHours, new DateOnly(2026, 8, 31), new DateOnly(2026, 12, 31))
        };

        // Act: record the transfer, then look at the balance in January, after the old period has expired
        ledger.AddRange(RequestLedger.Transfer(transfer, AccountId, new DateOnly(2026, 11, 18)));
        var balance = BalanceCalculator.Calculate(AccountType.Weeks1To5, ledger, new DateOnly(2027, 1, 5));

        // Assert: the transferred week is still available, and the rest of the old period has expired
        Assert.Equal(transferredHours, balance.TotalHours);
        Assert.Equal(earnedHours - transferredHours, balance.ExpiredHours); // 184.68 - 37 = 147.68
    }
}