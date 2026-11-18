using System.Globalization;
using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Domain.Rules;

// The rule values with their start dates. A value is looked up for a specific date, so a change
// (e.g. 2.08 to 2.16 days a month) only counts from its start date and old months keep the old value.
public class RuleSet
{
    private readonly ILookup<string, RuleParameter> _rules;

    public RuleSet(IEnumerable<RuleParameter> rules)
    {
        _rules = rules.ToLookup(r => r.Key);
    }

    // The value that was in force on the given date.
    public string Get(string key, DateOnly date) =>
        _rules[key]
            .Where(r => r.EffectiveFrom <= date)
            .MaxBy(r => r.EffectiveFrom)?.Value
        ?? throw new InvalidOperationException($"The rule '{key}' has no value on {date:yyyy-MM-dd}.");

    public decimal GetDecimal(string key, DateOnly date) =>
        decimal.Parse(Get(key, date), CultureInfo.InvariantCulture);

    public bool GetBool(string key, DateOnly date) =>
        bool.Parse(Get(key, date));
}
