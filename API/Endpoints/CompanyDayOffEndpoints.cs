using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Api.Validation;
using BusinessEsbjerg.Holiday.Service.Calendar;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

public record CalendarDayResponse(Guid? Id, DateOnly Date, string Name, bool IsPublicHoliday)
{
    public static CalendarDayResponse From(CalendarDay d) => new(d.Id, d.Date, d.Name, d.IsPublicHoliday);
}

public record NewCompanyDayOffDto(DateOnly Date, string Name);

public class NewCompanyDayOffDtoValidator : AbstractValidator<NewCompanyDayOffDto>
{
    public NewCompanyDayOffDtoValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public static class CompanyDayOffEndpoints
{
    public static void MapCompanyDayOffEndpoints(this IEndpointRouteBuilder app)
    {
        // only the system owner sets the company days off
        var group = app.MapGroup("/api/days-off").WithTags("Days off").RequireAuthorization(Policies.SystemOwner);

        group.MapGet("/", GetYear)
            .WithName("GetDaysOff")
            .WithSummary("Public holidays and company days off in a year");

        group.MapPost("/", Add)
            .WithValidation<NewCompanyDayOffDto>()
            .ProducesValidationProblem()
            .WithName("AddCompanyDayOff")
            .WithSummary("Add a company day off (only future dates)");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteCompanyDayOff")
            .WithSummary("Remove a future company day off");
    }

    // ?year=2027 in the address
    private static async Task<Ok<List<CalendarDayResponse>>> GetYear(int year, CompanyDaysOffService days,
        CancellationToken ct) =>
        TypedResults.Ok((await days.GetYearAsync(year, ct)).Select(CalendarDayResponse.From).ToList());

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Add(NewCompanyDayOffDto day,
        CompanyDaysOffService days, CancellationToken ct) =>
        ChangeResults.ToHttp(await days.AddAsync(day.Date, day.Name, ct));

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(Guid id,
        CompanyDaysOffService days, CancellationToken ct) =>
        ChangeResults.ToHttp(await days.DeleteAsync(id, ct));
}