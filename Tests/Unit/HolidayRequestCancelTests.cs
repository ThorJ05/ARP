using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class HolidayRequestCancelTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    private static HolidayRequest Request(RequestStatus status, DateOnly start) =>
        new() { Status = status, StartDate = start, EndDate = start.AddDays(4) };

    [Fact]
    public void Pending_is_cancelled()
    {
        var request = Request(RequestStatus.Pending, new DateOnly(2027, 1, 11));

        Assert.Null(request.Cancel(Today));
        Assert.Equal(RequestStatus.Cancelled, request.Status);
    }

    [Fact]
    public void Approved_needs_accountant()
    {
        var request = Request(RequestStatus.Approved, new DateOnly(2027, 1, 11));

        var error = request.Cancel(Today);

        Assert.Null(error);
        Assert.Equal(RequestStatus.CancellationRequested, request.Status);
    }

    [Fact]
    public void Started_cant_be_cancelled()
    {
        //Arrange 
        var request = Request(RequestStatus.Approved, new DateOnly(2026, 11, 16));

        //act   
        var error = request.Cancel(Today);
        
        //Assert 
        Assert.NotNull(error);
        Assert.Equal(RequestStatus.Approved, request.Status);
    }
    
    [Fact]
    public void Rejected_cant_be_cancelled()
    {
        //a
        var request = Request(RequestStatus.Rejected, new DateOnly(2027, 1, 11));
        
        //a
        var error = request.Cancel(Today);
        
        //a
        Assert.NotNull(error);
        
        Assert.Equal(RequestStatus.Rejected, request.Status);
    }
}
