using System.Security.Claims;
using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Service.Employees;
using BusinessEsbjerg.Holiday.Service.Requests;
using BusinessEsbjerg.Holiday.Service.Schedules;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

public record BalanceResponse(string Account, decimal Hours);

public record MyRequestResponse(
    Guid Id, string Account, DateOnly StartDate, DateOnly EndDate, decimal Hours,
    string Status, string? Comment, string? DecisionReason, DateTime CreatedAtUtc);

public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        // The employee id always comes from the token and never from the URL,
        // so an employee can only ever see their own data.
        var group = app.MapGroup("/api/me").WithTags("Me").RequireAuthorization(Policies.Employee);

        group.MapGet("/balances", GetMyBalances)
            .WithName("GetMyBalances")
            .WithSummary("My holiday balances in hours (weeks 1-5 and week 6)");

        group.MapGet("/requests", GetMyRequests)
            .WithName("GetMyRequests")
            .WithSummary("My holiday requests");

        // read only: there is no endpoint where employees can change their own schedule
        group.MapGet("/schedules", GetMySchedules)
            .WithName("GetMySchedules")
            .WithSummary("My work schedule history");
    }

    private static async Task<Ok<List<BalanceResponse>>> GetMyBalances(
        ClaimsPrincipal principal, EmployeeService employees, CancellationToken ct)
    {
        var balances = await employees.GetBalancesAsync(principal.GetEmployeeId()!.Value, ct);
        return TypedResults.Ok(balances.Select(b => new BalanceResponse(b.Type.ToString(), b.Hours)).ToList());
    }

    private static async Task<Ok<List<MyRequestResponse>>> GetMyRequests(
        ClaimsPrincipal principal, HolidayRequestService requests, CancellationToken ct)
    {
        var mine = await requests.GetForEmployeeAsync(principal.GetEmployeeId()!.Value, ct);
        return TypedResults.Ok(mine.Select(r => new MyRequestResponse(
            r.Id, r.AccountType.ToString(), r.StartDate, r.EndDate, r.Hours,
            r.Status.ToString(), r.Comment, r.DecisionReason, r.CreatedAtUtc)).ToList());
    }

    private static async Task<Ok<List<ScheduleResponse>>> GetMySchedules(
        ClaimsPrincipal principal, ScheduleService schedules, CancellationToken ct)
    {
        var history = await schedules.GetHistoryAsync(principal.GetEmployeeId()!.Value, ct) ?? [];
        return TypedResults.Ok(history.Select(ScheduleResponse.From).ToList());
    }
}
