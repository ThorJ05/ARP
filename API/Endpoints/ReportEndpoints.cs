using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Service.Reports;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        //only the accountant settles expired and leavers' hours
        var group = app.MapGroup("/api/reports").WithTags("Reports").RequireAuthorization(Policies.Accountant);

        group.MapGet("/holiday", GetHolidayReport)
            .WithName("GetHolidayReport")
            .WithSummary("Expired hours, and remaining hours for employees who are leaving");
    }
    
    private static async Task<Ok<IReadOnlyList<HolidayReportRow>>> GetHolidayReport(
        ReportService reports, CancellationToken ct) =>
        TypedResults.Ok(await reports.GetHolidayReportAsync(ct));
}