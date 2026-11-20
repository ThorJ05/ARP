using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Service.Balances;
using BusinessEsbjerg.Holiday.Service.Employees;

namespace BusinessEsbjerg.Holiday.Service.Reports;

// One line in the report: expired hours, and remaining hours if the employee is leaving.
public record HolidayReportRow(
    string Name, DateOnly? TerminationDate,
    decimal ExpiredWeeks1To5, decimal ExpiredWeek6,
    decimal? LeftWeeks1To5, decimal? LeftWeek6);

// The accountant's report: hours that must be paid out or settled.
public class ReportService(EmployeeService employees, BalanceService balances)
{
    public async Task<IReadOnlyList<HolidayReportRow>> GetHolidayReportAsync(CancellationToken ct = default)
    {
        var all = await employees.GetEmployeesAsync(ct);
        var expired = await balances.GetAllExpiredHoursAsync(ct);

        return all.Select(e => new HolidayReportRow(
                e.Name, e.TerminationDate,
                expired.GetValueOrDefault((e.Id, AccountType.Weeks1To5)),
                expired.GetValueOrDefault((e.Id, AccountType.Week6)),
                // remaining hours only matter for employees who are leaving
                e.TerminationDate is null ? null : e.Weeks1To5Hours,
                e.TerminationDate is null ? null : e.Week6Hours))
            .ToList();
    }
}