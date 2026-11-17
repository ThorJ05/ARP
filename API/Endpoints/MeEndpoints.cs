using System.Security.Claims;
using API.Auth;
using Service.Abstractions;

namespace API.Endpoints;

public record BalanceResponse(string Account, decimal Hours);

public record MyRequestResponse(
    Guid Id, string Account, DateOnly StartDate, DateOnly EndDate, decimal Hours,
    string Status, string? Comment, string? DecisionReason, DateTime CreatedAtUtc);

public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        // The employee id always comes from the token and never from the URL,
        // so an employee can only ever see their own balances and requests.
        var group = app.MapGroup("/api/me").WithTags("Me").RequireAuthorization(Policies.Employee);

        group.MapGet("/balances", async (ClaimsPrincipal principal, IEmployeeRepository employees, CancellationToken ct) =>
            {
                var balances = await employees.GetBalancesAsync(principal.GetEmployeeId()!.Value, ct);
                return TypedResults.Ok(balances.Select(b => new BalanceResponse(b.Type.ToString(), b.Hours)).ToList());
            })
            .WithName("GetMyBalances")
            .WithSummary("My holiday balances in hours (weeks 1-5 and week 6)");

        group.MapGet("/requests", async (ClaimsPrincipal principal, IEmployeeRepository employees, CancellationToken ct) =>
            {
                var requests = await employees.GetRequestsAsync(principal.GetEmployeeId()!.Value, ct);
                return TypedResults.Ok(requests.Select(r => new MyRequestResponse(
                    r.Id, r.AccountType.ToString(), r.StartDate, r.EndDate, r.Hours,
                    r.Status.ToString(), r.Comment, r.DecisionReason, r.CreatedAtUtc)).ToList());
            })
            .WithName("GetMyRequests")
            .WithSummary("My holiday requests");
    }
}
