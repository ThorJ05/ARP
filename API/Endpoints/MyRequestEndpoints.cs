using System.Security.Claims;
using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Api.Validation;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using BusinessEsbjerg.Holiday.Service.Requests;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

// Account is "Weeks1To5" or "Week6".
public record HolidayRequestDto(string Account, DateOnly StartDate, DateOnly EndDate, string? Comment)
{
    public NewHolidayRequest ToNewRequest() => new(Enum.Parse<AccountType>(Account), StartDate, EndDate);
}

public record RequestCheckResponse(decimal Hours, int WorkingDays, List<string> Errors)
{
    public static RequestCheckResponse From(RequestCheck c) => new(c.Hours, c.WorkingDays, c.Errors.ToList());
}

public record CreatedRequestResponse(Guid Id, decimal Hours);

public record MyRequestResponse(
    Guid Id, string Account, DateOnly StartDate, DateOnly EndDate, decimal Hours,
    string Status, string? Comment, string? DecisionReason, DateTime? DecidedAtUtc, DateTime CreatedAtUtc,
    bool CanCancel)
{
    public static MyRequestResponse From(RequestView view)
    {
        var r = view.Request;
        return new MyRequestResponse(r.Id, r.AccountType.ToString(), r.StartDate, r.EndDate, r.Hours,
            r.Status.ToString(), r.Comment, r.DecisionReason, r.DecidedAtUtc, r.CreatedAtUtc, view.CanCancel);
    }
}

public class HolidayRequestDtoValidator : AbstractValidator<HolidayRequestDto>
{
    public HolidayRequestDtoValidator()
    {
        RuleFor(x => x.Account)
            .Must(a => a is nameof(AccountType.Weeks1To5) or nameof(AccountType.Week6))
            .WithMessage("Account must be Weeks1To5 or Week6.");
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public static class MyRequestEndpoints
{
    public static void MapMyRequestEndpoints(this IEndpointRouteBuilder app)
    {
        // the employee id comes from the token, so employees only see and change their own requests
        var group = app.MapGroup("/api/me/requests").WithTags("Me").RequireAuthorization(Policies.Employee);

        group.MapGet("/", GetMyRequests)
            .WithName("GetMyRequests")
            .WithSummary("My holiday requests and their status");

        group.MapPost("/preview", Preview)
            .WithValidation<HolidayRequestDto>()
            .ProducesValidationProblem()
            .WithName("PreviewRequest")
            .WithSummary("The hour cost of a request and the rules it breaks, without sending it");

        group.MapPost("/", Create)
            .WithValidation<HolidayRequestDto>()
            .ProducesValidationProblem()
            .WithName("CreateRequest")
            .WithSummary("Send a holiday request to the accountant");

        group.MapPost("/{id:guid}/cancel", Cancel)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithName("CancelRequest")
            .WithSummary("Withdraw a pending request, or ask to cancel approved holiday that hasn't started");
    }

    private static async Task<Ok<List<MyRequestResponse>>> GetMyRequests(
        ClaimsPrincipal principal, HolidayRequestService requests, CancellationToken ct)
    {
        var mine = await requests.GetForEmployeeAsync(principal.GetEmployeeId()!.Value, ct);
        return TypedResults.Ok(mine.Select(MyRequestResponse.From).ToList());
    }

    private static async Task<Results<Ok<RequestCheckResponse>, NotFound>> Preview(
        HolidayRequestDto request, ClaimsPrincipal principal, HolidayRequestService requests, CancellationToken ct)
    {
        var check = await requests.PreviewAsync(principal.GetEmployeeId()!.Value, request.ToNewRequest(), ct);
        return check is null ? TypedResults.NotFound() : TypedResults.Ok(RequestCheckResponse.From(check));
    }

    private static async Task<Results<Created<CreatedRequestResponse>, ValidationProblem, NotFound>> Create(
        HolidayRequestDto request, ClaimsPrincipal principal, HolidayRequestService requests, CancellationToken ct)
    {
        var created = await requests.CreateAsync(
            principal.GetEmployeeId()!.Value, request.ToNewRequest(), request.Comment, ct);

        if (created is null) return TypedResults.NotFound();
        if (created.RequestId is not { } id)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = created.Check.Errors.ToArray()
            });

        return TypedResults.Created($"/api/me/requests/{id}", new CreatedRequestResponse(id, created.Check.Hours));
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Cancel(
        Guid id, ClaimsPrincipal principal, HolidayRequestService requests, CancellationToken ct) =>
        ChangeResults.ToHttp(await requests.CancelAsync(principal.GetEmployeeId()!.Value, id, ct));
}
