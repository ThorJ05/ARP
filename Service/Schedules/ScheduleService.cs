using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Service.Abstractions;

namespace BusinessEsbjerg.Holiday.Service.Schedules;

/// <summary>A schedule with the last day it was used (null = still in use).</summary>
public record ScheduleView(
    Guid Id, DateOnly ValidFrom, DateOnly? ValidTo, WorkWeek Week, decimal WeeklyHours, decimal AverageDayHours);

/// <summary>
/// Work schedules. A schedule is never edited: a change is a new schedule from a date, so the history
/// stays complete. Only accounting and the system owner may add schedules (checked by the API).
/// </summary>
public class ScheduleService(IEmployeeRepository employees, IScheduleRepository schedules, ILedgerRepository ledger)
{
    /// <summary>The full schedule history, oldest first (null if the employee doesn't exist).</summary>
    public async Task<IReadOnlyList<ScheduleView>?> GetHistoryAsync(Guid employeeId, CancellationToken ct = default)
    {
        if (await employees.GetByIdAsync(employeeId, ct) is null) return null;
        return ToViews(await schedules.GetForEmployeeAsync(employeeId, ct));
    }

    private static IReadOnlyList<ScheduleView> ToViews(IEnumerable<WorkScheduleEntry> entries)
    {
        var ordered = entries.OrderBy(s => s.ValidFrom).ToList();

        // a schedule is used until the day before the next one starts
        return ordered.Select((s, i) => new ScheduleView(
                s.Id, s.ValidFrom,
                i + 1 < ordered.Count ? ordered[i + 1].ValidFrom.AddDays(-1) : null,
                WorkWeek.From(s), s.WeeklyHours, s.AverageDayHours))
            .ToList();
    }

    /// <summary>
    /// Adds a new schedule from a date. The remaining balance is converted so the number of days stays
    /// the same (e.g. 74 h at 7.4 h/day = 10 days = 75 h at 7.5 h/day). The conversion is saved as a new
    /// ledger entry, so nothing old is changed.
    /// </summary>
    public async Task<ChangeResult> AddScheduleAsync(Guid employeeId, DateOnly validFrom, WorkWeek week,
        CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct);
        if (employee is null) return ChangeResult.Missing;

        if (validFrom < employee.HireDate)
            return ChangeResult.Invalid("A schedule can't start before the hire date.");
        if (employee.TerminationDate is { } end && validFrom > end)
            return ChangeResult.Invalid("A schedule can't start after the end date.");

        var current = (await schedules.GetForEmployeeAsync(employeeId, ct)).MaxBy(s => s.ValidFrom);
        if (current is not null && validFrom <= current.ValidFrom)
            return ChangeResult.Invalid(
                $"The new schedule must start after the latest one (from {current.ValidFrom:yyyy-MM-dd}). Old schedules are never changed.");

        var schedule = week.ToSchedule(employeeId, validFrom);
        var conversions = current is null
            ? []
            : await ConvertBalancesAsync(employeeId, current, schedule, ct);

        await schedules.AddAsync(schedule, conversions, ct);
        return ChangeResult.Ok;
    }

    // one ledger entry per account with the difference between the old and the converted balance
    private async Task<List<LedgerEntry>> ConvertBalancesAsync(Guid employeeId, WorkScheduleEntry from,
        WorkScheduleEntry to, CancellationToken ct)
    {
        var conversions = new List<LedgerEntry>();
        if (from.AverageDayHours == 0m) return conversions;

        var balances = await ledger.GetBalancesAsync(employeeId, before: to.ValidFrom, ct: ct);
        foreach (var balance in balances.Where(b => b.Hours != 0m))
        {
            var difference = WorkScheduleEntry.ConvertBalance(balance.Hours, from, to) - balance.Hours;
            if (difference == 0m) continue;

            conversions.Add(new LedgerEntry
            {
                AccountId = balance.AccountId,
                Type = LedgerEntryType.ScheduleConversion,
                Hours = difference,
                Date = to.ValidFrom,
                Reason = $"Schedule changed from {from.AverageDayHours} to {to.AverageDayHours} hours a day"
            });
        }

        return conversions;
    }
}
