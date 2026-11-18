namespace BusinessEsbjerg.Holiday.Domain.Entities;

// Full-time or part-time employee. Hourly paid employees are not in the system
// because they get their holiday pay through Feriekonto.
public class Employee
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = "";
    public DateOnly HireDate { get; set; }
    public DateOnly? TerminationDate { get; set; }

    public List<WorkScheduleEntry> WorkSchedule { get; set; } = new();
    public List<HolidayAccount> Accounts { get; set; } = new();
    public List<HolidayRequest> Requests { get; set; } = new();
    public List<TransferRequest> TransferRequests { get; set; } = new();

    // Finds the work schedule that was used on the given date. WorkSchedule must be loaded.
    public WorkScheduleEntry? GetScheduleFor(DateOnly date) =>
        WorkSchedule
            .Where(w => w.ValidFrom <= date)
            .OrderByDescending(w => w.ValidFrom)
            .FirstOrDefault();

    // How many hours the employee works on the given date.
    public decimal GetHoursFor(DateOnly date) =>
        GetScheduleFor(date)?.GetHours(date.DayOfWeek) ?? 0m;

    // What a holiday from start to end (both included) costs in hours: each day costs the hours of that
    // weekday in the schedule used on the day, so a short Friday costs less than a long Tuesday.
    // Weekends cost nothing, and neither do the days in daysOff (public holidays and company days off).
    public decimal GetHolidayHours(DateOnly start, DateOnly end, IReadOnlySet<DateOnly>? daysOff = null)
    {
        var hours = 0m;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (daysOff?.Contains(day) != true)
                hours += GetHoursFor(day);
        }

        return hours;
    }
}
