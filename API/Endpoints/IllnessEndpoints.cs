using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Api.Validation;
using BusinessEsbjerg.Holiday.Service.Requests;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

public record ApprovedHolidayResponse(
    Guid Id, string EmployeeName, string Account, DateOnly StartDate, DateOnly EndDate, decimal Hours)
{
    public static ApprovedHolidayResponse From(ApprovedHoliday h) =>
        new(h.Id, h.EmployeeName, h.Account.ToString(), h.StartDate, h.EndDate, h.Hours);
}

public record IllnessDto(DateOnly FirstSickDay, DateOnly LastSickDay);

public record IllnessResponse(decimal HoursGivenBack);

public class IllnessDtoValidator : AbstractValidator<IllnessDto>
{
    public IllnessDtoValidator()
    {
        RuleFor(x => x.FirstSickDay).NotEmpty();
        RuleFor(x => x.LastSickDay).NotEmpty()
            .GreaterThanOrEqualTo(x => x.FirstSickDay).WithMessage("The last sick day can't be before the first.");
    }
}

public static class IllnessEndpoints
{
    public static void MapIllnessEndpoints(this IEndpointRouteBuilder app)
    {
        // only the accountant registers illness
        var group = app.MapGroup("/api/illness").WithTags("Illness").RequireAuthorization(Policies.Accountant);

        group.MapGet("/holidays", GetApprovedHoliday)
            .WithName("GetApprovedHoliday")
            .WithSummary("Approved holiday that illness can be registered on");

        group.MapPost("/holidays/{id:guid}", RegisterIllness)
            .WithValidation<IllnessDto>()
            .ProducesValidationProblem()
            .WithName("RegisterIllness")
            .WithSummary("Register sick days during approved holiday, the days after the waiting days are given back");
    }

    private static async Task<Ok<List<ApprovedHolidayResponse>>> GetApprovedHoliday(IllnessService illness,
        CancellationToken ct) =>
        TypedResults.Ok((await illness.GetApprovedHolidayAsync(ct)).Select(ApprovedHolidayResponse.From).ToList());

    private static async Task<Results<Ok<IllnessResponse>, NotFound, ProblemHttpResult>> RegisterIllness(Guid id,
        IllnessDto request, IllnessService illness, CancellationToken ct)
    {
        var result = await illness.RegisterAsync(id, request.FirstSickDay, request.LastSickDay, ct);
        if (result.Change.NotFound) return TypedResults.NotFound();
        if (result.Change.Error is { } error)
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Not allowed", detail: error);
        return TypedResults.Ok(new IllnessResponse(result.HoursGivenBack));
    }
}
