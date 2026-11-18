using API.Auth;
using API.Validation;
using BusinessEsbjerg.Holiday.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Service.Users;

namespace API.Endpoints;

public record CreateEmployeeRequest(string Name, string Email, string Password, DateOnly HireDate, WorkWeek Schedule);
public record CreatedEmployeeResponse(Guid EmployeeId, Guid UserId);

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

public class WorkWeekValidator : AbstractValidator<WorkWeek>
{
    public WorkWeekValidator()
    {
        Day("Monday", w => w.MondayStart, w => w.MondayEnd);
        Day("Tuesday", w => w.TuesdayStart, w => w.TuesdayEnd);
        Day("Wednesday", w => w.WednesdayStart, w => w.WednesdayEnd);
        Day("Thursday", w => w.ThursdayStart, w => w.ThursdayEnd);
        Day("Friday", w => w.FridayStart, w => w.FridayEnd);

        RuleFor(w => w)
            .Must(w => w.MondayStart is not null || w.TuesdayStart is not null || w.WednesdayStart is not null
                       || w.ThursdayStart is not null || w.FridayStart is not null)
            .WithName("Schedule")
            .WithMessage("The schedule needs at least one working day.");
    }

    // a day needs both start and end (or none), and end must be after start
    private void Day(string name, Func<WorkWeek, TimeOnly?> start, Func<WorkWeek, TimeOnly?> end) =>
        RuleFor(w => w)
            .Must(w => start(w) is null ? end(w) is null : end(w) > start(w))
            .WithName(name)
            .WithMessage($"{name}: give both a start and an end time (or none), and the end must be after the start.");
}

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization(Policies.SystemOwner);

        group.MapPost("/employees", CreateEmployee)
            .WithValidation<CreateEmployeeRequest>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithName("CreateEmployee")
            .WithSummary("Create an employee with a login and a work schedule (system owner only)");
    }

    private static async Task<Results<Created<CreatedEmployeeResponse>, ProblemHttpResult>> CreateEmployee(
        CreateEmployeeRequest request, UserService users, CancellationToken ct)
    {
        var result = await users.CreateEmployeeAsync(
            new NewEmployee(request.Name, request.Email, request.Password, request.HireDate, request.Schedule), ct);

        if (result.EmailTaken)
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "The email is already in use");

        return TypedResults.Created((string?)null, new CreatedEmployeeResponse(result.EmployeeId, result.UserId));
    }
}
