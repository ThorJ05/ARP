using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using Service.Abstractions;
using Service.Users;

namespace Service.Schedules;

/// <summary>A schedule with the last day it was used (null = still in use).</summary>
public record ScheduleView(
    Guid Id, DateOnly ValidFrom, DateOnly? ValidTo, WorkWeek Week, decimal WeeklyHours, decimal AverageDayHours);

public record EmployeeView(
    Guid Id, string Name, string? Email, DateOnly HireDate, DateOnly? TerminationDate,
    decimal? CurrentWeeklyHours, decimal Weeks1To5Hours, decimal Week6Hours);

/// <summary>NotFound = the employee doesn't exist, Error = the change isn't allowed.</summary>
public record ChangeResult(bool NotFound, string? Error)
{
    public static readonly ChangeResult Ok = new(false, null);
    public static readonly ChangeResult Missing = new(true, null);
    public static ChangeResult Invalid(string error) => new(false, error);
}

/// <summary>
/// Work schedules and end dates. A schedule is never edited: a change is a new schedule from a date,
/// so the history stays complete. Only accounting and the system owner may do this (checked by the API).
/// </summary>
public class ScheduleService(IEmployeeRepository employees, IClock clock)
{
    public async Task<IReadOnlyList<EmployeeView>> GetEmployeesAsync(CancellationToken ct = default)
    {
        var today = clock.Today;
        var all = await employees.GetAllAsync(ct);

        return all.Select(e => new EmployeeView(
                e.Employee.Id, e.Employee.Name, e.Email, e.Employee.HireDate, e.Employee.TerminationDate,
                e.Employee.GetScheduleFor(today)?.WeeklyHours,
                e.Balances.FirstOrDefault(b => b.Type == AccountType.Weeks1To5)?.Hours ?? 0m,
                e.Balances.FirstOrDefault(b => b.Type == AccountType.Week6)?.Hours ?? 0m))
            .ToList();
    }

    /// <summary>The full schedule history, oldest first (null if the employee doesn't exist).</summary>
    public async Task<IReadOnlyList<ScheduleView>?> GetHistoryAsync(Guid employeeId, CancellationToken ct = default)
    {
        if (await employees.GetByIdAsync(employeeId, ct) is null) return null;
        return ToViews(await employees.GetSchedulesAsync(employeeId, ct));
    }

    public static IReadOnlyList<ScheduleView> ToViews(IEnumerable<WorkScheduleEntry> schedules)
    {
        var ordered = schedules.OrderBy(s => s.ValidFrom).ToList();

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

        var schedules = await employees.GetSchedulesAsync(employeeId, ct);
        var current = schedules.MaxBy(s => s.ValidFrom);
        if (current is not null && validFrom <= current.ValidFrom)
            return ChangeResult.Invalid(
                $"The new schedule must start after the latest one (from {current.ValidFrom:yyyy-MM-dd}). Old schedules are never changed.");

        var schedule = week.ToSchedule(employeeId, validFrom);
        var conversions = new List<LedgerEntry>();

        if (current is not null && current.AverageDayHours > 0m)
        {
            var balances = await employees.GetBalancesAsync(employeeId, before: validFrom, ct: ct);
            foreach (var balance in balances.Where(b => b.Hours != 0m))
            {
                var difference = WorkScheduleEntry.ConvertBalance(balance.Hours, current, schedule) - balance.Hours;
                if (difference == 0m) continue;

                conversions.Add(new LedgerEntry
                {
                    AccountId = balance.AccountId,
                    Type = LedgerEntryType.ScheduleConversion,
                    Hours = difference,
                    Date = validFrom,
                    Reason = $"Schedule changed from {current.AverageDayHours} to {schedule.AverageDayHours} hours a day"
                });
            }
        }

        await employees.AddScheduleAsync(schedule, conversions, ct);
        return ChangeResult.Ok;
    }

    /// <summary>Sets (or removes, with null) the last day of employment.</summary>
    public async Task<ChangeResult> SetTerminationDateAsync(Guid employeeId, DateOnly? terminationDate,
        CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct);
        if (employee is null) return ChangeResult.Missing;

        if (terminationDate is { } end)
        {
            if (end < employee.HireDate)
                return ChangeResult.Invalid("The end date can't be before the hire date.");

            var schedules = await employees.GetSchedulesAsync(employeeId, ct);
            if (schedules.Any(s => s.ValidFrom > end))
                return ChangeResult.Invalid("There is a work schedule that starts after this end date.");
        }

        await employees.SetTerminationDateAsync(employeeId, terminationDate, ct);
        return ChangeResult.Ok;
    }
}
