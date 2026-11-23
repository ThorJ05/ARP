using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Rules;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class RuleChangeTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly DateOnly NextYear = new(2027, 1, 1);

    private static List<RuleParameter> Existing(string key, string value) => [Rule(key, value)];

    [Fact]
    public void A_valid_future_value_is_accepted()
    {
        var errors = RuleChange.Check(Existing(RuleKeys.Weeks1To5DaysPerMonth, "2.08"), "2.16", NextYear, Today);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("2,16")]
    [InlineData("-1")]
    public void A_number_rule_only_accepts_numbers_of_0_or_more(string value)
    {
        var errors = RuleChange.Check(Existing(RuleKeys.Weeks1To5DaysPerMonth, "2.08"), value, NextYear, Today);

        Assert.Contains(errors, e => e.Contains("number"));
    }

    [Theory]
    [InlineData("ja")]
    [InlineData("1")]
    public void A_true_false_rule_only_accepts_true_or_false(string value)
    {
        var errors = RuleChange.Check(Existing(RuleKeys.Week6Enabled, "true"), value, NextYear, Today);

        Assert.Contains(errors, e => e.Contains("true or false"));
    }

    [Theory]
    [InlineData("31-12")]
    [InlineData("12/31")]
    [InlineData("02-29")]
    public void A_date_rule_only_accepts_a_real_MM_dd_date(string value)
    {
        var errors = RuleChange.Check(Existing(RuleKeys.TransferDeadlineWeek5, "12-31"), value, NextYear, Today);

        Assert.Contains(errors, e => e.Contains("MM-dd"));
    }

    [Fact]
    public void Today_or_earlier_is_rejected_so_the_past_is_never_changed()
    {
        var existing = Existing(RuleKeys.Weeks1To5DaysPerMonth, "2.08");

        Assert.NotEmpty(RuleChange.Check(existing, "2.16", Today, Today));
        Assert.NotEmpty(RuleChange.Check(existing, "2.16", new DateOnly(2025, 1, 1), Today));
    }

    [Fact]
    public void Two_values_from_the_same_date_are_rejected()
    {
        List<RuleParameter> existing =
        [
            Rule(RuleKeys.TransferMaxDays, "5"),
            Rule(RuleKeys.TransferMaxDays, "4", NextYear)
        ];

        var errors = RuleChange.Check(existing, "3", NextYear, Today);

        Assert.Contains(errors, e => e.Contains("already has a value"));
    }

    [Fact]
    public void An_empty_value_is_rejected()
    {
        var errors = RuleChange.Check(Existing(RuleKeys.TransferMaxDays, "5"), " ", NextYear, Today);

        Assert.Contains(errors, e => e.Contains("write a value"));
    }
}
