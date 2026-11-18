using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Service.Abstractions;

namespace BusinessEsbjerg.Holiday.Service.Employees;

public record EmployeeView(
    Guid Id, string Name, string? Email, DateOnly HireDate, DateOnly? TerminationDate,
    decimal? CurrentWeeklyHours, decimal Weeks1To5Hours, decimal Week6Hours);

/// <summary>The employee list, end dates and balances.</summary>
public class EmployeeService(
    IEmployeeRepository employees, IScheduleRepository schedules, ILedgerRepository ledger, IClock clock)
{
    public async Task<IReadOnlyList<EmployeeView>> GetEmployeesAsync(CancellationToken ct = default)
    {
        var all = await employees.GetAllAsync(ct);
        var allSchedules = await schedules.GetAllAsync(ct);
        var balances = await ledger.GetAllBalancesAsync(ct);
        var today = clock.Today;

        return all.Select(e =>
            {
                var employee = e.Employee;
                employee.WorkSchedule = allSchedules.Where(s => s.EmployeeId == employee.Id).ToList();

                decimal Hours(AccountType type) =>
                    balances.FirstOrDefault(b => b.EmployeeId == employee.Id && b.Type == type)?.Hours ?? 0m;

                return new EmployeeView(
                    employee.Id, employee.Name, e.Email, employee.HireDate, employee.TerminationDate,
                    employee.GetScheduleFor(today)?.WeeklyHours,
                    Hours(AccountType.Weeks1To5), Hours(AccountType.Week6));
            })
            .ToList();
    }

    public Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(Guid employeeId, CancellationToken ct = default) =>
        ledger.GetBalancesAsync(employeeId, ct: ct);

    /// <summary>Sets (or removes, with null) the last day of employment. Only the system owner (checked by the API).</summary>
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
        }

        await employees.SetTerminationDateAsync(employeeId, terminationDate, ct);
        return ChangeResult.Ok;
    }
}
