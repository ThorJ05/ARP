using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Requests;

namespace BusinessEsbjerg.Holiday.Domain.Absence;

// Which working days an employee is away, calculated from holiday requests.
// Weekends, daysOff (public holidays, company days off) and days the employee doesn't work are never absence.
public static class AbsenceCalendar
{
    // The requests must belong to the employee, and employee.WorkSchedule must be loaded.
    public static IReadOnlySet<DateOnly> AbsentDays(Employee employee, IEnumerable<HolidayRequest> requests,
        DateOnly from, DateOnly to, IReadOnlySet<DateOnly> daysOff)
    {
        var days = new HashSet<DateOnly>();
        foreach (var request in requests)
        {
            // only the part of the request that is inside the period
            var start = request.StartDate > from ? request.StartDate : from;
            var end = request.EndDate < to ? request.EndDate : to;
            if (start > end) continue;

            foreach (var day in HolidayDays.Between(employee, start, end, daysOff))
                days.Add(day.Date);
        }

        return days;
    }

    // How many employees are away on each day.
    public static IReadOnlyDictionary<DateOnly, int> CountPerDay(IEnumerable<IEnumerable<DateOnly>> absentDaysPerEmployee)
    {
        var counts = new Dictionary<DateOnly, int>();
        foreach (var days in absentDaysPerEmployee)
        foreach (var day in days)
            counts[day] = counts.GetValueOrDefault(day) + 1;
        return counts;
    }
}
