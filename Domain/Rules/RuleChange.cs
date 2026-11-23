using System.Globalization;
using System.Text.RegularExpressions;
using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Domain.Rules;

// Checks a new rule value before it is saved. Rule values are stored as text, and a wrong value
// (e.g. "abc" where a number is expected) would make every calculation that reads the rule fail.
// So a new value must be the same kind as the rule's current value: a number, true/false or a date "MM-dd".
public static partial class RuleChange
{
    // The values the rule already has, the new value and its start date. Returns the broken checks (empty = OK).
    public static IReadOnlyList<string> Check(IReadOnlyList<RuleParameter> existing, string value,
        DateOnly effectiveFrom, DateOnly today)
    {
        var errors = new List<string>();

        // only future dates, so current and past holiday is never changed
        if (effectiveFrom <= today)
            errors.Add("The start date must be after today, so current and past holiday isn't changed.");

        if (existing.Any(r => r.EffectiveFrom == effectiveFrom))
            errors.Add("This rule already has a value from that date.");

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("You must write a value.");
            return errors;
        }

        var latest = existing.MaxBy(r => r.EffectiveFrom);
        if (latest is not null && TypeError(latest.Value, value.Trim()) is { } typeError)
            errors.Add(typeError);

        return errors;
    }

    // Null if the value is the same kind as the current value, otherwise what is wrong.
    public static string? TypeError(string current, string value) => KindOf(current) switch
    {
        RuleKind.TrueFalse when !bool.TryParse(value, out _) =>
            "This rule must be true or false.",
        RuleKind.Number when !IsNumber(value) =>
            "This rule must be a number of 0 or more, e.g. 2.08 (use a dot for decimals).",
        RuleKind.MonthDay when !IsMonthDay(value) =>
            "This rule must be a date written as MM-dd, e.g. 12-31.",
        _ => null
    };

    private enum RuleKind { TrueFalse, Number, MonthDay, Text }

    private static RuleKind KindOf(string value) =>
        bool.TryParse(value, out _) ? RuleKind.TrueFalse
        : IsMonthDay(value) ? RuleKind.MonthDay
        : IsNumber(value) ? RuleKind.Number
        : RuleKind.Text;

    // no minus sign and a dot for decimals, the same format RuleSet.GetDecimal reads
    private static bool IsNumber(string value) =>
        decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _);

    // e.g. "12-31"; checked against a year that isn't a leap year, so "02-29" is not allowed
    private static bool IsMonthDay(string value) =>
        MonthDayFormat().IsMatch(value)
        && DateOnly.TryParseExact($"2001-{value}", "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _);

    [GeneratedRegex(@"^\d{2}-\d{2}$")]
    private static partial Regex MonthDayFormat();
}
