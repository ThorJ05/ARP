using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Rules;

namespace BusinessEsbjerg.Holiday.Domain.Balances;

// Finds the ledger entries that are missing for an employee up to a date:
// one accrual per account per month, and a conversion per account when a new schedule starts.
// Entries that already exist are skipped, so it can run as often as we like.
// The same code is used to save entries up to today and to show a balance on a future date.
public static class AccrualPlanner
{
    private record Event(DateOnly Date, DateOnly? Month, WorkScheduleEntry? From, WorkScheduleEntry? To);

    // the employee must have WorkSchedule loaded, and existing is all the ledger entries they already have
    public static IReadOnlyList<LedgerEntry> Plan(Employee employee, IReadOnlyList<HolidayAccount> accounts,
        IReadOnlyList<LedgerEntry> existing, RuleSet rules, DateOnly until)
    {
        var done = existing
            .Where(e => e.PeriodMonth is not null)
            .Select(e => (e.AccountId, e.Type, e.PeriodMonth!.Value))
            .ToHashSet();
        var all = existing.ToList();
        var planned = new List<LedgerEntry>();

        foreach (var e in Events(employee, until))
        {
            var entries = e.Month is { } month
                ? Accruals(employee, accounts, rules, month, done)
                : Conversions(accounts, all, e.Date, e.From!, e.To!, done);

            all.AddRange(entries);
            planned.AddRange(entries);
        }

        return planned;
    }

    // months (dated the day they are earned) and schedule changes, in date order
    private static IEnumerable<Event> Events(Employee employee, DateOnly until)
    {
        var events = new List<Event>();

        for (var month = new DateOnly(employee.HireDate.Year, employee.HireDate.Month, 1); ; month = month.AddMonths(1))
        {
            if (employee.TerminationDate is { } end && month > end) break;

            var lastDay = month.AddMonths(1).AddDays(-1);
            var earnedOn = employee.TerminationDate is { } leave && leave < lastDay ? leave : lastDay;
            if (earnedOn > until) break;

            events.Add(new Event(earnedOn, month, null, null));
        }

        var schedules = employee.WorkSchedule.OrderBy(s => s.ValidFrom).ToList();
        for (var i = 1; i < schedules.Count; i++)
        {
            if (schedules[i].ValidFrom <= until)
                events.Add(new Event(schedules[i].ValidFrom, null, schedules[i - 1], schedules[i]));
        }

        // a conversion before the accrual on the same day, because it is about the balance before that day
        return events.OrderBy(e => e.Date).ThenBy(e => e.Month is null ? 0 : 1);
    }

    private static List<LedgerEntry> Accruals(Employee employee, IEnumerable<HolidayAccount> accounts, RuleSet rules,
        DateOnly month, HashSet<(Guid, LedgerEntryType, DateOnly)> done)
    {
        var missing = accounts.Where(a => !done.Contains((a.Id, LedgerEntryType.Accrual, month))).ToList();
        if (missing.Count == 0) return [];

        var accrual = AccrualCalculator.ForMonth(employee, month, rules);
        if (accrual is null) return [];

        return missing
            .Select(account => new LedgerEntry
            {
                AccountId = account.Id,
                Type = LedgerEntryType.Accrual,
                Hours = account.Type == AccountType.Weeks1To5 ? accrual.Weeks1To5Hours : accrual.Week6Hours,
                Date = accrual.EarnedOn,
                PeriodMonth = month,
                ExpiresOn = HolidayPeriods.ExpiresOn(account.Type, month),
                Reason = $"Earned in {month:yyyy-MM}"
            })
            .Where(entry => entry.Hours != 0m)
            .ToList();
    }

    // the employee keeps the same number of days, so the hours change with the new day length
    private static List<LedgerEntry> Conversions(IEnumerable<HolidayAccount> accounts, List<LedgerEntry> all,
        DateOnly date, WorkScheduleEntry from, WorkScheduleEntry to, HashSet<(Guid, LedgerEntryType, DateOnly)> done)
    {
        if (from.AverageDayHours == 0m || from.AverageDayHours == to.AverageDayHours) return [];

        var conversions = new List<LedgerEntry>();
        foreach (var account in accounts)
        {
            if (done.Contains((account.Id, LedgerEntryType.ScheduleConversion, date))) continue;

            var before = all.Where(e => e.AccountId == account.Id && e.Date < date);
            var hours = BalanceCalculator.Calculate(account.Type, before, date).TotalHours;
            var difference = WorkScheduleEntry.ConvertBalance(hours, from, to) - hours;
            if (difference == 0m) continue;

            conversions.Add(new LedgerEntry
            {
                AccountId = account.Id,
                Type = LedgerEntryType.ScheduleConversion,
                Hours = difference,
                Date = date,
                PeriodMonth = date,
                Reason = $"Schedule changed from {from.AverageDayHours} to {to.AverageDayHours} hours a day"
            });
        }

        return conversions;
    }
}
