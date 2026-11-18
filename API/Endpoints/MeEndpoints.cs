using System.Security.Claims;
using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Service.Balances;
using BusinessEsbjerg.Holiday.Service.Schedules;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        // The employee id always comes from the token and never from the URL,
        // so an employee can only ever see their own data.
        var group = app.MapGroup("/api/me").WithTags("Me").RequireAuthorization(Policies.Employee);

        group.MapGet("/balances", GetMyBalances)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithName("GetMyBalances")
            .WithSummary("My holiday balances in hours and days with expiry dates, today or on a future date");

        // read only: there is no endpoint where employees can change their own schedule
        group.MapGet("/schedules", GetMySchedules)
            .WithName("GetMySchedules")
            .WithSummary("My work schedule history");
    }

    private static Task<Results<Ok<List<BalanceResponse>>, NotFound, ProblemHttpResult>> GetMyBalances(
        ClaimsPrincipal principal, BalanceService balances, DateOnly? date, CancellationToken ct) =>
        BalanceResults.GetAsync(balances, principal.GetEmployeeId()!.Value, date, ct);

    private static async Task<Ok<List<ScheduleResponse>>> GetMySchedules(
        ClaimsPrincipal principal, ScheduleService schedules, CancellationToken ct)
    {
        var history = await schedules.GetHistoryAsync(principal.GetEmployeeId()!.Value, ct) ?? [];
        return TypedResults.Ok(history.Select(ScheduleResponse.From).ToList());
    }
}
