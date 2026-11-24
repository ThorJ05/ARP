using System.Net;
using System.Net.Http.Json;
using BusinessEsbjerg.Holiday.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using static BusinessEsbjerg.Holiday.Tests.TestDates;

namespace BusinessEsbjerg.Holiday.Tests.Integration;

[Collection(ApiCollection.Name)]
public class HolidayRequestTests(ApiFactory api)
{
    private const string Weeks1To5 = "Weeks1To5";

    private static async Task<decimal> Weeks1To5HoursOn(HttpClient employee, DateOnly date)
    {
        var url = $"/api/me/balances?date={date:yyyy-MM-dd}";
        var balances = await employee.GetFromJsonAsync<List<BalanceResponse>>(url);
        return balances!.Single(b => b.Account == Weeks1To5).Hours;
    }

    [Fact]
    public async Task Approval_reduces_balance()
    {
        // Arrange: Anna asks for a week off
        var anna = await api.LoggedInAs(TestUsers.Anna);
        var accountant = await api.LoggedInAs(TestUsers.Accountant);
        var start = MondayInWeeks(6);
        var end = start.AddDays(4);
        var before = await Weeks1To5HoursOn(anna, end);

        var sent = await anna.PostAsJsonAsync("/api/me/requests", new HolidayRequestDto(Weeks1To5, start, end, null));
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        var request = await sent.Content.ReadFromJsonAsync<CreatedRequestResponse>();

        // Act: the accountant approves it
        var approved = await accountant.PostAsJsonAsync($"/api/requests/holiday/{request!.Id}/approve",
            new DecisionDto(null));

        // Assert: the hours are gone from the balance and Anna sees the request as approved
        Assert.Equal(HttpStatusCode.NoContent, approved.StatusCode);
        Assert.Equal(before - request.Hours, await Weeks1To5HoursOn(anna, end));

        var mine = await anna.GetFromJsonAsync<List<MyRequestResponse>>("/api/me/requests");
        Assert.Equal("Approved", mine!.Single(r => r.Id == request.Id).Status);
    }

    [Fact]
    public async Task Broken_rule_gives_400()
    {
        // Arrange: the end date is before the start date
        var anna = await api.LoggedInAs(TestUsers.Anna);
        var start = MondayInWeeks(8);
        var request = new HolidayRequestDto(Weeks1To5, start, start.AddDays(-3), null);

        // Act
        var response = await anna.PostAsJsonAsync("/api/me/requests", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains("The end date can't be before the start date.", problem!.Errors["request"]);
    }

    [Fact]
    public async Task Cant_cancel_others_request()
    {
        // Arrange: Anna has a request waiting
        var anna = await api.LoggedInAs(TestUsers.Anna);
        var bo = await api.LoggedInAs(TestUsers.Bo);
        var start = MondayInWeeks(10);
        var sent = await anna.PostAsJsonAsync("/api/me/requests",
            new HolidayRequestDto(Weeks1To5, start, start.AddDays(4), null));
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        var request = await sent.Content.ReadFromJsonAsync<CreatedRequestResponse>();

        // Act: Bo tries to cancel it
        var response = await bo.PostAsync($"/api/me/requests/{request!.Id}/cancel", null);

        // Assert: Bo can't even see it, and it is still waiting
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var mine = await anna.GetFromJsonAsync<List<MyRequestResponse>>("/api/me/requests");
        Assert.Equal("Pending", mine!.Single(r => r.Id == request.Id).Status);
    }
}
