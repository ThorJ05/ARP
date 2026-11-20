using System.Security.Claims;
using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Service.Overview;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

public record TeamRowResponse(Guid EmployeeId, string Name, bool IsMe, List<DateOnly> AbsentDays);

public record AbsenceCountResponse(DateOnly Date, int Absent);

// No balances anywhere in this response: colleagues must never see them.
public record TeamOverviewResponse(
    int Year, int Month, List<DateOnly> DaysOff, List<TeamRowResponse> Rows, List<AbsenceCountResponse> AbsentPerDay);

public record MyCalendarDayResponse(DateOnly Date, string Status, string Account);

public record MyCalendarResponse(int Year, int Month, List<DateOnly> DaysOff, List<MyCalendarDayResponse> Days);

public static class OverviewEndpoints
{
    public static void MapOverviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/overview").WithTags("Overview").RequireAuthorization(Policies.Employee);

        group.MapGet("/team", GetTeam)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithName("GetTeamOverview")
            .WithSummary("Who is away each day of a month, and how many are away. Never shows balances");

        // the employee id comes from the token, never from the URL
        group.MapGet("/me", GetMine)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithName("GetMyCalendar")
            .WithSummary("My own holiday in a month, including requests that are waiting for approval");
    }

    private static async Task<Results<Ok<TeamOverviewResponse>, ProblemHttpResult>> GetTeam(
        ClaimsPrincipal principal, OverviewService overview, int year, int month, CancellationToken ct)
    {
        if (!TryRange(year, month, out var from, out var to)) return InvalidMonth();

        var team = await overview.GetTeamAsync(from, to, ct);
        var me = principal.GetEmployeeId();
        return TypedResults.Ok(new TeamOverviewResponse(
            year, month,
            team.DaysOff.Order().ToList(),
            team.Rows.Select(r => new TeamRowResponse(r.EmployeeId, r.Name, r.EmployeeId == me, r.AbsentDays.ToList()))
                .ToList(),
            team.AbsentPerDay.OrderBy(p => p.Key).Select(p => new AbsenceCountResponse(p.Key, p.Value)).ToList()));
    }

    private static async Task<Results<Ok<MyCalendarResponse>, NotFound, ProblemHttpResult>> GetMine(
        ClaimsPrincipal principal, OverviewService overview, int year, int month, CancellationToken ct)
    {
        if (!TryRange(year, month, out var from, out var to)) return InvalidMonth();

        var calendar = await overview.GetMineAsync(principal.GetEmployeeId()!.Value, from, to, ct);
        if (calendar is null) return TypedResults.NotFound();

        return TypedResults.Ok(new MyCalendarResponse(
            year, month,
            calendar.DaysOff.Order().ToList(),
            calendar.Days.Select(d => new MyCalendarDayResponse(d.Date, d.Status.ToString(), d.Account.ToString()))
                .ToList()));
    }

    private static bool TryRange(int year, int month, out DateOnly from, out DateOnly to)
    {
        from = default;
        to = default;
        if (year is < 2000 or > 2100 || month is < 1 or > 12) return false;

        from = new DateOnly(year, month, 1);
        to = from.AddMonths(1).AddDays(-1);
        return true;
    }

    private static ProblemHttpResult InvalidMonth() =>
        TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Not allowed",
            detail: "The year or the month is not valid.");
}
