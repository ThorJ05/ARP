using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Rules;
using BusinessEsbjerg.Holiday.Service.Abstractions;

namespace BusinessEsbjerg.Holiday.Service.Rules;

// One rule with all its values, newest first (the history).
public record RuleHistory(string Key, string? Description, IReadOnlyList<RuleParameter> Values);

// The system owner's rules. A change is always a new value with a start date, old values are kept.
// The checks are in Domain (RuleChange, RulePresets), so they can be unit tested without a database.
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

    // Adds a new value from a start date. Missing if the rule doesn't exist, Invalid if a check fails.
    public async Task<ChangeResult> ChangeAsync(string key, string value, DateOnly effectiveFrom,
        CancellationToken ct = default)
    {
        var existing = (await rules.GetAllAsync(ct)).Where(r => r.Key == key).ToList();
        if (existing.Count == 0) return ChangeResult.Missing;

        var errors = RuleChange.Check(existing, value, effectiveFrom, clock.Today);
        if (errors.Count > 0) return ChangeResult.Invalid(string.Join(" ", errors));

        await rules.AddAsync(new RuleParameter
        {
            Key = key,
            Value = value.Trim(),
            EffectiveFrom = effectiveFrom,
            Description = existing[0].Description
        }, ct);

        return ChangeResult.Ok;
    }

    // The presets the system owner can choose from.
    public IReadOnlyList<RulePreset> GetPresets() => RulePresets.All;

    // Adds the preset's values from a start date, as new values (old values are kept).
    // All checks run first, so either all values are added or none.
    public async Task<ChangeResult> ApplyPresetAsync(string presetId, DateOnly effectiveFrom,
        CancellationToken ct = default)
    {
        var preset = RulePresets.Find(presetId);
        if (preset is null) return ChangeResult.Missing;

        var all = await rules.GetAllAsync(ct);
        var errors = RulePresets.Check(preset, all, effectiveFrom, clock.Today);
        if (errors.Count > 0) return ChangeResult.Invalid(string.Join(" ", errors));

        // values that are already the same are skipped, so the history only shows real changes
        foreach (var (key, value) in RulePresets.ValuesToAdd(preset, all, effectiveFrom))
        {
            await rules.AddAsync(new RuleParameter
            {
                Key = key,
                Value = value,
                EffectiveFrom = effectiveFrom,
                Description = all.First(r => r.Key == key).Description
            }, ct);
        }

        return ChangeResult.Ok;
    }
}
