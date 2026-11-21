using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Api.Validation;
using BusinessEsbjerg.Holiday.Domain.Rules;
using BusinessEsbjerg.Holiday.Service.Rules;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

// What the API sends back (I give you these, since they're just data)
public record RuleValueResponse(string Value, DateOnly EffectiveFrom);

public record RuleResponse(string Key, string? Description, List<RuleValueResponse> Values)
{
    public static RuleResponse From(RuleHistory h) => new(h.Key, h.Description,
        h.Values.Select(v => new RuleValueResponse(v.Value, v.EffectiveFrom)).ToList());
}

// A new value for a rule, from a start date.
public record RuleChangeDto(string Value, DateOnly EffectiveFrom);

public class RuleChangeDtoValidator : AbstractValidator<RuleChangeDto>
{
    public RuleChangeDtoValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EffectiveFrom).NotEmpty();
    }
}

public record RulePresetResponse(string Id, string Name, string Description, Dictionary<string, string> Values)
{
    public static RulePresetResponse From(RulePreset p) =>
        new(p.Id, p.Name, p.Description, p.Values.ToDictionary());
}

// Which date the preset's values apply from.
public record ApplyPresetDto(DateOnly EffectiveFrom);

public class ApplyPresetDtoValidator : AbstractValidator<ApplyPresetDto>
{
    public ApplyPresetDtoValidator()
    {
        RuleFor(x => x.EffectiveFrom).NotEmpty();
    }
}


public static class RuleEndpoints
{
    public static void MapRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/rules").WithTags("rules").RequireAuthorization(Policies.SystemOwner);

        group.MapGet("/", GetRules)
            .WithName("GetRules")
            .WithSummary("Every rule with its history, newest value first");

        group.MapPost("/{key}", ChangeRule)
            .WithValidation<RuleChangeDto>()
            .ProducesValidationProblem()
            .WithName("ChangeRule")
            .WithSummary("Add a new value for a rule from a start date (old values are kept)");
        
        group.MapGet("/presets", GetPresets)
            .WithName("GetRulePresets")
            .WithSummary("The rule presets the system owner can apply");

        group.MapPost("/presets/{id}", ApplyPreset)
            .WithValidation<ApplyPresetDto>()
            .ProducesValidationProblem()
            .WithName("ApplyRulePreset")
            .WithSummary("Apply a preset from a start date (adds new values, old values are kept)");
    }

    // return all rules as RuleResponse
    private static async Task<Ok<List<RuleResponse>>> GetRules(RuleService rules, CancellationToken ct) =>
        TypedResults.Ok((await rules.GetAllAsync(ct)).Select(RuleResponse.From).ToList());

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ChangeRule(string key,
        RuleChangeDto change,
        RuleService rules, CancellationToken ct) =>
        ChangeResults.ToHttp(await rules.ChangeAsync(key, change.Value, change.EffectiveFrom, ct));
    
    private static Ok<List<RulePresetResponse>> GetPresets(RuleService rules) =>
        TypedResults.Ok(rules.GetPresets().Select(RulePresetResponse.From).ToList());

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ApplyPreset(string id,
        ApplyPresetDto apply, RuleService rules, CancellationToken ct) =>
        ChangeResults.ToHttp(await rules.ApplyPresetAsync(id, apply.EffectiveFrom, ct));
}