using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class HolidayRequestCancelTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    private static HolidayRequest Request(RequestStatus status, DateOnly start) =>
        new() { Status = status, StartDate = start, EndDate = start.AddDays(4) };

    [Fact]
    public void A_pending_request_is_cancelled_right_away()
    {
        var request = Request(RequestStatus.Pending, new DateOnly(2027, 1, 11));

        Assert.Null(request.Cancel(Today));
        Assert.Equal(RequestStatus.Cancelled, request.Status);
    }

    [Fact]
    public void Approved_holiday_that_hasnt_started_needs_the_accountant()
    {
        var request = Request(RequestStatus.Approved, new DateOnly(2027, 1, 11));

        Assert.Null(request.Cancel(Today));
        Assert.Equal(RequestStatus.CancellationRequested, request.Status);
    }

    [Fact]
    public void Holiday_that_has_started_cant_be_cancelled()
    {
        var request = Request(RequestStatus.Approved, new DateOnly(2026, 11, 16));

        Assert.NotNull(request.Cancel(Today));
        Assert.Equal(RequestStatus.Approved, request.Status);
    }

    [Fact]
    public void A_rejected_request_cant_be_cancelled()
    {
        var request = Request(RequestStatus.Rejected, new DateOnly(2027, 1, 11));

        Assert.NotNull(request.Cancel(Today));
    }
}
