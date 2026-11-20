using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Rules;

namespace BusinessEsbjerg.Holiday.Domain.Requests;

// Illness during approved holiday. The holiday days the employee was sick are given back, except the
// first waiting days (a rule, 5 today). Simplified: the waiting days are counted per holiday request.
public class HolidayIllness(HolidayRequest request, IReadOnlyList<LedgerEntry> entries, RuleSet rules)
{
    public IReadOnlyList<string> Check(DateOnly firstSickDay, DateOnly lastSickDay)
    {
        var errors = new List<string>();
        if (request.Status != RequestStatus.Approved)
            errors.Add("Illness can only be registered on approved holiday.");
        if (lastSickDay < firstSickDay)
            errors.Add("The last sick day can't be before the first.");
        if (firstSickDay < request.StartDate || lastSickDay > request.EndDate)
            errors.Add($"The sick days must be within the holiday " +
                       $"({request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}).");

        if (errors.Count == 0 && DaysToGiveBack(firstSickDay, lastSickDay).Count == 0)
        {
            errors.Add($"Nothing to give back. The first {WaitingDays(firstSickDay)} sick holiday days are not given " +
                       "back, and days already given back can't be given back again.");
        }

        return errors;
    }

    // The holiday days used in the sick period, without the waiting days and days already given back.
    public IReadOnlyList<LedgerEntry> DaysToGiveBack(DateOnly firstSickDay, DateOnly lastSickDay)
    {
        var givenBack = entries
            .Where(e => e.RequestId == request.Id && e.Type == LedgerEntryType.Illness)
            .Select(e => e.Date)
            .ToHashSet();

        return entries
            .Where(e => e.RequestId == request.Id && e.Type == LedgerEntryType.Usage
                        && e.Date >= firstSickDay && e.Date <= lastSickDay)
            .OrderBy(e => e.Date)
            .Skip(WaitingDays(firstSickDay))
            .Where(e => !givenBack.Contains(e.Date))
            .ToList();
    }

    private int WaitingDays(DateOnly date) => (int)rules.GetDecimal(RuleKeys.IllnessWaitingDays, date);
}
