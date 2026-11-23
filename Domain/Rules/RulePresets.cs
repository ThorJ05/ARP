using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Domain.Rules;

// A named set of rule values the system owner can apply from a start date.
// Values is rule key → value, e.g. "week6.enabled" → "false".
public record RulePreset(string Id, string Name, string Description, IReadOnlyDictionary<string, string> Values);

// The presets the system ships with. A preset is a starting point: the system owner can change
// single values afterwards, which makes it a custom rule set.
public static class RulePresets
{
    public static readonly IReadOnlyList<RulePreset> All =
    [
        new RulePreset(
            "minimum",
            "Ferieloven (minimum)",
            "Only the 5 weeks from the Holiday Act, no 6th holiday week.",
            new Dictionary<string, string>
            {
                [RuleKeys.Weeks1To5DaysPerMonth] = "2.08",
                [RuleKeys.Week6Enabled] = "false"
            }),

        new RulePreset(
            "kl-0511",
            "Ferieloven + 6. ferieuge (KL 05.11)",
            "The 5 weeks plus the 6th holiday week. What Business Esbjerg uses.",
            new Dictionary<string, string>
            {
                [RuleKeys.Weeks1To5DaysPerMonth] = "2.08",
                [RuleKeys.Week6Enabled] = "true",
                [RuleKeys.Week6HoursPerMonth] = "3.083"
            })
    ];

    // The preset with this id, or null if there is none.
    public static RulePreset? Find(string id) => All.FirstOrDefault(p => p.Id == id);

    // Checks a preset before it is applied from a start date (all rules = every rule value in the database).
    // Everything is checked first, so either all the values are added or none.
    public static IReadOnlyList<string> Check(RulePreset preset, IReadOnlyList<RuleParameter> allRules,
        DateOnly effectiveFrom, DateOnly today)
    {
        var errors = new List<string>();

        // only future dates, the same as when one value is changed
        if (effectiveFrom <= today)
            errors.Add("The start date must be after today, so current and past holiday isn't changed.");

        foreach (var (key, value) in preset.Values)
        {
            var values = allRules.Where(r => r.Key == key).ToList();
            if (values.Count == 0)
            {
                errors.Add($"The rule {key} doesn't exist.");
                continue;
            }

            if (values.Any(r => r.EffectiveFrom == effectiveFrom))
                errors.Add($"The rule {key} already has a value from that date.");

            // the same type check as a single change, so a preset can't break a rule either
            if (RuleChange.TypeError(values.MaxBy(r => r.EffectiveFrom)!.Value, value) is { } typeError)
                errors.Add($"{key}: {typeError}");
        }

        return errors;
    }

    // The preset's values that differ from what already applies on the start date, so the history
    // only shows real changes. Call Check first.
    public static IReadOnlyList<KeyValuePair<string, string>> ValuesToAdd(RulePreset preset,
        IReadOnlyList<RuleParameter> allRules, DateOnly effectiveFrom)
    {
        var current = new RuleSet(allRules);
        return preset.Values.Where(v => current.Get(v.Key, effectiveFrom) != v.Value).ToList();
    }
}