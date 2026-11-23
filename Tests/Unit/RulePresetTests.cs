using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Rules;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class RulePresetTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly DateOnly NextYear = new(2027, 1, 1);

    // Business Esbjerg's values today (the KL 05.11 model)
    private static List<RuleParameter> CurrentRules() =>
    [
        Rule(RuleKeys.Weeks1To5DaysPerMonth, "2.08"),
        Rule(RuleKeys.Week6Enabled, "true"),
        Rule(RuleKeys.Week6HoursPerMonth, "3.083")
    ];

    private static RulePreset Preset(string id) => RulePresets.Find(id)!;

    [Fact]
    public void Presets_are_valid()
    {
        foreach (var preset in RulePresets.All)
            Assert.Empty(RulePresets.Check(preset, CurrentRules(), NextYear, Today));
    }

    [Fact]
    public void Adds_only_changes()
    {
        var values = RulePresets.ValuesToAdd(Preset("minimum"), CurrentRules(), NextYear);

        var value = Assert.Single(values);
        Assert.Equal(RuleKeys.Week6Enabled, value.Key);
        Assert.Equal("false", value.Value);
    }

    [Fact]
    public void Preset_in_use_adds_nothing()
    {
        Assert.Empty(RulePresets.ValuesToAdd(Preset("kl-0511"), CurrentRules(), NextYear));
    }

    [Fact]
    public void Past_date_rejected()
    {
        Assert.NotEmpty(RulePresets.Check(Preset("minimum"), CurrentRules(), Today, Today));
    }

    [Fact]
    public void Clashing_date_rejected()
    {
        var rules = CurrentRules();
        rules.Add(Rule(RuleKeys.Week6Enabled, "false", NextYear));

        var errors = RulePresets.Check(Preset("kl-0511"), rules, NextYear, Today);

        Assert.Contains(errors, e => e.Contains("already has a value"));
    }

    [Fact]
    public void Unknown_preset_not_found()
    {
        Assert.Null(RulePresets.Find("does-not-exist"));
    }
}
