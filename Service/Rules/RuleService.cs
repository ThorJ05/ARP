using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Service.Abstractions;
// A CLASS TO RULE THEM ALL
namespace BusinessEsbjerg.Holiday.Service.Rules;

// One rule with all its values, newest first (the history).
public record RuleHistory(string Key, string? Description, IReadOnlyList<RuleParameter> Values);

// The system owner's rules. A change is always a new value with a start date, old values are kept.
public class RuleService(IRuleRepository rules, IClock clock)
{
    public async Task<IReadOnlyList<RuleHistory>> GetAllAsync(CancellationToken ct = default)
    {
        var all = await rules.GetAllAsync(ct);
        return all
            .GroupBy(r => r.Key)
            .OrderBy(g => g.Key)
            .Select(g => new RuleHistory(
                g.Key,
                g.First().Description,
                g.OrderByDescending(r => r.EffectiveFrom).ToList()))
            .ToList();
    }

    public async Task<ChangeResult> ChangeAsync(string key, string value, DateOnly effectiveFrom,
        CancellationToken ct = default)
    {
        var exixting = (await rules.GetAllAsync(ct)).Where(r => r.Key == key).ToList();
        if (exixting.Count == 0) return ChangeResult.Missing;
        // Only future dates, so current and past holidays is never changed
        if (effectiveFrom <= clock.Today)
            return ChangeResult.Invalid("The start date must be after today, so current and past holiday isn't changed");
        // The rule must not already have a value from that exact date.
        if (exixting.Any(r => r.EffectiveFrom == effectiveFrom))
            return ChangeResult.Invalid("This rule already has a value from that date.");
        // The value must not be empty
       if (string.IsNullOrWhiteSpace(value))
        return ChangeResult.Invalid("You must write a value.");



        await rules.AddAsync(new RuleParameter()
        {
            Key = key,
            Value = value.Trim(),
            EffectiveFrom = effectiveFrom,
            Description = exixting[0].Description
        }, ct);
        
        return ChangeResult.Ok;
    }
}