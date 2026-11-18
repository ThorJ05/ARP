using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using BusinessEsbjerg.Holiday.Service.Balances;

namespace BusinessEsbjerg.Holiday.Service.Schedules;

// A schedule with the last day it was used (null = still in use).
public record ScheduleView(
    Guid Id, DateOnly ValidFrom, DateOnly? ValidTo, WorkWeek Week, decimal WeeklyHours, decimal AverageDayHours);

// Work schedules. A schedule is never edited: a change is a new schedule from a date, so the history
// stays complete. Only accounting and the system owner may add schedules (checked by the API).
public class ScheduleService(IEmployeeRepository employees, IScheduleRepository schedules, BalanceService balances)
{
    // The full schedule history, oldest first (null if the employee doesn't exist).
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

    // Adds a new schedule from a date. The remaining balance is converted on that date so the number of
    // days stays the same (e.g. 74 h at 7.4 h/day = 10 days = 75 h at 7.5 h/day). The conversion is
    // saved as a new ledger entry by the balance service, so nothing old is changed.
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

        if (await balances.GetLastAccrualDateAsync(employeeId, ct) is { } accruedUntil && validFrom <= accruedUntil)
            return ChangeResult.Invalid(
                $"Holiday has already been earned up to {accruedUntil:yyyy-MM-dd}, so the new schedule must start after that day.");

        await schedules.AddAsync(week.ToSchedule(employeeId, validFrom), ct);

        // saves the conversion right away if the schedule starts today or earlier
        await balances.CatchUpAsync(employeeId, ct);
        return ChangeResult.Ok;
    }
}
