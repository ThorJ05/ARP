using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using BusinessEsbjerg.Holiday.Service.Balances;

namespace BusinessEsbjerg.Holiday.Service.Employees;

public record EmployeeView(
    Guid Id, string Name, string? Email, DateOnly HireDate, DateOnly? TerminationDate,
    decimal? CurrentWeeklyHours, decimal Weeks1To5Hours, decimal Week6Hours);

// The employee list and end dates.
public class EmployeeService(
    IEmployeeRepository employees, IScheduleRepository schedules, BalanceService balances, IClock clock)
{
    public async Task<IReadOnlyList<EmployeeView>> GetEmployeesAsync(CancellationToken ct = default)
    {
        var hours = await balances.GetAllCurrentHoursAsync(ct);
        var all = await employees.GetAllAsync(ct);
        var schedulesByEmployee = (await schedules.GetAllAsync(ct)).ToLookup(s => s.EmployeeId);
        var today = clock.Today;

        return all.Select(e =>
            {
                var employee = e.Employee;
                employee.WorkSchedule = schedulesByEmployee[employee.Id].ToList();

                return new EmployeeView(
                    employee.Id, employee.Name, e.Email, employee.HireDate, employee.TerminationDate,
                    employee.GetScheduleFor(today)?.WeeklyHours,
                    hours.GetValueOrDefault((employee.Id, AccountType.Weeks1To5)),
                    hours.GetValueOrDefault((employee.Id, AccountType.Week6)));
            })
            .ToList();
    }

    // Sets (or removes, with null) the last day of employment. Only the system owner (checked by the API).
    public async Task<ChangeResult> SetTerminationDateAsync(Guid employeeId, DateOnly? terminationDate,
        CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct);
        if (employee is null) return ChangeResult.Missing;

        if (terminationDate is { } end)
        {
            if (end < employee.HireDate)
                return ChangeResult.Invalid("The end date can't be before the hire date.");

            var employeeSchedules = await schedules.GetForEmployeeAsync(employeeId, ct);
            if (employeeSchedules.Any(s => s.ValidFrom > end))
                return ChangeResult.Invalid("There is a work schedule that starts after this end date.");

            if (await balances.GetLastAccrualDateAsync(employeeId, ct) is { } accruedUntil && end < accruedUntil)
                return ChangeResult.Invalid(
                    $"Holiday has already been earned up to {accruedUntil:yyyy-MM-dd}, so the end date can't be before that day.");
        }

        await employees.SetTerminationDateAsync(employeeId, terminationDate, ct);
        return ChangeResult.Ok;
    }
}
