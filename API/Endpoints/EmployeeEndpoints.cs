using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Api.Validation;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Service;
using BusinessEsbjerg.Holiday.Service.Balances;
using BusinessEsbjerg.Holiday.Service.Employees;
using BusinessEsbjerg.Holiday.Service.Schedules;
using BusinessEsbjerg.Holiday.Service.Users;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

public record CreateEmployeeRequest(string Name, string Email, string Password, DateOnly HireDate, WorkWeekDto Schedule);
public record CreatedEmployeeResponse(Guid EmployeeId, Guid UserId);
public record NewScheduleRequest(DateOnly ValidFrom, WorkWeekDto Schedule);
public record TerminationRequest(DateOnly? TerminationDate);

public record EmployeeResponse(
    Guid Id, string Name, string? Email, DateOnly HireDate, DateOnly? TerminationDate,
    decimal? CurrentWeeklyHours, decimal Weeks1To5Hours, decimal Week6Hours)
{
    public static EmployeeResponse From(EmployeeView e) => new(
        e.Id, e.Name, e.Email, e.HireDate, e.TerminationDate, e.CurrentWeeklyHours, e.Weeks1To5Hours, e.Week6Hours);
}

public record ScheduleResponse(
    Guid Id, DateOnly ValidFrom, DateOnly? ValidTo, WorkWeekDto Week, decimal WeeklyHours, decimal AverageDayHours)
{
    public static ScheduleResponse From(ScheduleView s) => new(
        s.Id, s.ValidFrom, s.ValidTo, WorkWeekDto.From(s.Week), s.WeeklyHours, s.AverageDayHours);
}

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200)
            .Must(email => email.Trim().EndsWith(User.CompanyEmailDomain, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"Email must end with {User.CompanyEmailDomain}.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(200);
        RuleFor(x => x.HireDate).NotEmpty();
        RuleFor(x => x.Schedule).NotNull().SetValidator(new WorkWeekValidator());
    }
}

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

        group.MapGet("/", GetEmployees)
            .WithName("GetEmployees")
            .WithSummary("All employees with weekly hours and balances (accountant and system owner)");

        group.MapPost("/", CreateEmployee)
            .RequireAuthorization(Policies.SystemOwner) // on top of the group policy: only the system owner
            .WithValidation<CreateEmployeeRequest>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithName("CreateEmployee")
            .WithSummary("Create an employee with a login and a work schedule (system owner only)");

        group.MapGet("/{id:guid}/balances", GetBalances)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithName("GetEmployeeBalances")
            .WithSummary("An employee's balances with expiry dates, today or on a future date (accountant and system owner)");

        group.MapGet("/{id:guid}/schedules", GetHistory)
            .WithName("GetScheduleHistory")
            .WithSummary("The full work schedule history of an employee (accountant and system owner)");

        group.MapPost("/{id:guid}/schedules", AddSchedule)
            .WithValidation<NewScheduleRequest>()
            .ProducesValidationProblem()
            .WithName("AddSchedule")
            .WithSummary("Give an employee a new schedule from a date (accountant and system owner)");

        group.MapPut("/{id:guid}/termination", SetTermination)
            .RequireAuthorization(Policies.SystemOwner)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithName("SetTerminationDate")
            .WithSummary("Set or remove the last day of employment (system owner only)");
    }

    private static async Task<Ok<List<EmployeeResponse>>> GetEmployees(EmployeeService employees, CancellationToken ct) =>
        TypedResults.Ok((await employees.GetEmployeesAsync(ct)).Select(EmployeeResponse.From).ToList());

    private static async Task<Results<Created<CreatedEmployeeResponse>, ProblemHttpResult>> CreateEmployee(
        CreateEmployeeRequest request, UserService users, CancellationToken ct)
    {
        var result = await users.CreateEmployeeAsync(
            new NewEmployee(request.Name, request.Email, request.Password, request.HireDate, request.Schedule.ToWorkWeek()),
            ct);

        if (result.EmailTaken)
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "The email is already in use");

        return TypedResults.Created((string?)null, new CreatedEmployeeResponse(result.EmployeeId, result.UserId));
    }

    private static async Task<Results<Ok<List<ScheduleResponse>>, NotFound>> GetHistory(
        Guid id, ScheduleService schedules, CancellationToken ct)
    {
        var history = await schedules.GetHistoryAsync(id, ct);
        if (history is null) return TypedResults.NotFound();
        return TypedResults.Ok(history.Select(ScheduleResponse.From).ToList());
    }

    private static Task<Results<Ok<List<BalanceResponse>>, NotFound, ProblemHttpResult>> GetBalances(
        Guid id, DateOnly? date, BalanceService balances, CancellationToken ct) =>
        BalanceResults.GetAsync(balances, id, date, ct);

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> AddSchedule(
        Guid id, NewScheduleRequest request, ScheduleService schedules, CancellationToken ct) =>
        ChangeResults.ToHttp(await schedules.AddScheduleAsync(id, request.ValidFrom, request.Schedule.ToWorkWeek(), ct));

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> SetTermination(
        Guid id, TerminationRequest request, EmployeeService employees, CancellationToken ct) =>
        ChangeResults.ToHttp(await employees.SetTerminationDateAsync(id, request.TerminationDate, ct));

}
