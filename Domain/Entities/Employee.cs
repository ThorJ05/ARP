namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>
/// Full-time or part-time employee. Hourly paid employees are not in the system
/// because they get their holiday pay through Feriekonto.
/// </summary>
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

    /// <summary>Finds the work schedule that was used on the given date. WorkSchedule must be loaded.</summary>
    public WorkScheduleEntry? GetScheduleFor(DateOnly date) =>
        WorkSchedule
            .Where(w => w.ValidFrom <= date)
            .OrderByDescending(w => w.ValidFrom)
            .FirstOrDefault();

    /// <summary>How many hours the employee works on the given date.</summary>
    public decimal GetHoursFor(DateOnly date) =>
        GetScheduleFor(date)?.GetHours(date.DayOfWeek) ?? 0m;
}
