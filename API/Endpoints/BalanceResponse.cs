using BusinessEsbjerg.Holiday.Service.Balances;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

public record BalanceBucketResponse(DateOnly AvailableFrom, DateOnly ExpiresOn, decimal Hours, decimal Days);

// The balance of one account on a date. Hours is everything not expired, AvailableHours is what can be
// taken on the date (week 6 can't be used until 1 May the year after). Buckets split it by expiry date.
public record BalanceResponse(
    string Account, DateOnly Date, decimal DayHours,
    decimal Hours, decimal Days, decimal AvailableHours, decimal AvailableDays, decimal ExpiredHours,
    List<BalanceBucketResponse> Buckets)
{
    public static BalanceResponse From(BalanceView view)
    {
        var b = view.Balance;
        return new BalanceResponse(
            b.Type.ToString(), b.Date, view.DayHours,
            b.TotalHours, view.ToDays(b.TotalHours), b.AvailableHours, view.ToDays(b.AvailableHours), b.ExpiredHours,
            b.Buckets.Select(x => new BalanceBucketResponse(x.AvailableFrom, x.ExpiresOn, x.Hours, view.ToDays(x.Hours)))
                .ToList());
    }
}

// Shared by /api/me/balances and /api/employees/{id}/balances.
public static class BalanceResults
{
    public static async Task<Results<Ok<List<BalanceResponse>>, NotFound, ProblemHttpResult>> GetAsync(
        BalanceService balances, Guid employeeId, DateOnly? date, CancellationToken ct)
    {
        if (date is { } day && balances.IsTooFarAhead(day))
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Not allowed",
                detail: $"A balance can be shown at most {BalanceService.MaxYearsAhead} years ahead.");

        var views = await balances.GetBalancesAsync(employeeId, date, ct);
        return views is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(views.Select(BalanceResponse.From).ToList());
    }
}
