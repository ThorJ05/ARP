using API.Auth;
using API.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Service.Schedules;
using Service.Users;

namespace API.Endpoints;

public record NewScheduleRequest(DateOnly ValidFrom, WorkWeek Schedule);
public record TerminationRequest(DateOnly? TerminationDate);

public class NewScheduleRequestValidator : AbstractValidator<NewScheduleRequest>
{
    public NewScheduleRequestValidator()
    {
        RuleFor(x => x.ValidFrom).NotEmpty();
        RuleFor(x => x.Schedule).NotNull().SetValidator(new WorkWeekValidator());
    }
}

public static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employees").WithTags("Employees")
            .RequireAuthorization(Policies.AccountantOrSystemOwner);

        group.MapGet("/", async (ScheduleService schedules, CancellationToken ct) =>
                TypedResults.Ok(await schedules.GetEmployeesAsync(ct)))
            .WithName("GetEmployees")
            .WithSummary("All employees with weekly hours and balances (accountant and system owner)");

        group.MapGet("/{id:guid}/schedules", GetHistory)
            .WithName("GetScheduleHistory")
            .WithSummary("The full work schedule history of an employee (accountant and system owner)");

        group.MapPost("/{id:guid}/schedules", AddSchedule)
            .WithValidation<NewScheduleRequest>()
            .ProducesValidationProblem()
            .WithName("AddSchedule")
            .WithSummary("Give an employee a new schedule from a date (accountant and system owner)");

        group.MapPut("/{id:guid}/termination", SetTermination)
            .RequireAuthorization(Policies.SystemOwner) // on top of the group policy: only the system owner
            .WithName("SetTerminationDate")
            .WithSummary("Set or remove the last day of employment (system owner only)");
    }

    private static async Task<Results<Ok<IReadOnlyList<ScheduleView>>, NotFound>> GetHistory(
        Guid id, ScheduleService schedules, CancellationToken ct)
    {
        var history = await schedules.GetHistoryAsync(id, ct);
        if (history is null) return TypedResults.NotFound();
        return TypedResults.Ok(history);
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> AddSchedule(
        Guid id, NewScheduleRequest request, ScheduleService schedules, CancellationToken ct) =>
        ToHttp(await schedules.AddScheduleAsync(id, request.ValidFrom, request.Schedule, ct));

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> SetTermination(
        Guid id, TerminationRequest request, ScheduleService schedules, CancellationToken ct) =>
        ToHttp(await schedules.SetTerminationDateAsync(id, request.TerminationDate, ct));

    private static Results<NoContent, NotFound, ProblemHttpResult> ToHttp(ChangeResult result)
    {
        if (result.NotFound) return TypedResults.NotFound();
        if (result.Error is not null)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Not allowed", detail: result.Error);
        return TypedResults.NoContent();
    }
}
