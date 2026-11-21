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
                [RuleKeys.Week6Enabled] = "false",
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
}