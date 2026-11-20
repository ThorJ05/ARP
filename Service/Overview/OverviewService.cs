using BusinessEsbjerg.Holiday.Domain.Absence;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using BusinessEsbjerg.Holiday.Service.Calendar;

namespace BusinessEsbjerg.Holiday.Service.Overview;

// One employee in the team grid. Only name and absent days: balances are never part of the overview.
public record TeamRow(Guid EmployeeId, string Name, IReadOnlyList<DateOnly> AbsentDays);

public record TeamOverview(
    DateOnly From, DateOnly To, IReadOnlySet<DateOnly> DaysOff, IReadOnlyList<TeamRow> Rows,
    IReadOnlyDictionary<DateOnly, int> AbsentPerDay);

public record MyCalendarDay(DateOnly Date, RequestStatus Status, AccountType Account);

public record MyCalendar(DateOnly From, DateOnly To, IReadOnlySet<DateOnly> DaysOff, IReadOnlyList<MyCalendarDay> Days);

// Who is away when. Everyone can see everyone's approved holiday, but never anyone's balance.
public class OverviewService(
    IEmployeeRepository employees, IScheduleRepository schedules, IHolidayRequestRepository requests,
    DaysOffService daysOff)
{
    // colleagues see holiday that is approved (approved stays booked until a cancellation is approved)
    private static readonly RequestStatus[] ShownToColleagues = [RequestStatus.Approved, RequestStatus.CancellationRequested];

    public async Task<TeamOverview> GetTeamAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var off = await daysOff.GetBetweenAsync(from, to, ct);

        // employees who worked here at some point in the period
        var people = (await employees.GetAllAsync(ct))
            .Select(e => e.Employee)
            .Where(e => e.HireDate <= to && (e.TerminationDate is null || e.TerminationDate >= from))
            .ToList();

        var schedulesByEmployee = (await schedules.GetAllAsync(ct)).ToLookup(s => s.EmployeeId);
        var approved = (await requests.GetByStatusAsync(ShownToColleagues, ct))
            .Where(r => r.StartDate <= to && r.EndDate >= from)
            .ToLookup(r => r.EmployeeId);

        var rows = new List<TeamRow>();
        foreach (var person in people)
        {
            person.WorkSchedule = schedulesByEmployee[person.Id].ToList();
            var days = AbsenceCalendar.AbsentDays(person, approved[person.Id], from, to, off);
            rows.Add(new TeamRow(person.Id, person.Name, days.Order().ToList()));
        }

        return new TeamOverview(from, to, off, rows, AbsenceCalendar.CountPerDay(rows.Select(r => r.AbsentDays)));
    }

    // Null if the employee doesn't exist.
    public async Task<MyCalendar?> GetMineAsync(Guid employeeId, DateOnly from, DateOnly to,
        CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(employeeId, ct);
        if (employee is null) return null;
        employee.WorkSchedule = (await schedules.GetForEmployeeAsync(employeeId, ct)).ToList();

        var off = await daysOff.GetBetweenAsync(from, to, ct);
        var mine = (await requests.GetForEmployeeAsync(employeeId, ct))
            .Where(r => r.IsActive() && r.StartDate <= to && r.EndDate >= from);

        var days = new List<MyCalendarDay>();
        foreach (var request in mine)
        foreach (var date in AbsenceCalendar.AbsentDays(employee, [request], from, to, off))
            days.Add(new MyCalendarDay(date, request.Status, request.AccountType));

        return new MyCalendar(from, to, off, days.OrderBy(d => d.Date).ToList());
    }
}
