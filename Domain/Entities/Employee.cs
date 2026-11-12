using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateOnly HireDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public PayType PayType { get; set; } = PayType.Salaried;

    public List<WorkScheduleEntry> WorkSchedule { get; set; } = new();
    public List<HolidayAccount> Accounts { get; set; } = new();
    public List<HolidayRequest> Requests { get; set; } = new();
    public List<MonthlySalaryRecord> SalaryRecords { get; set; } = new();

    /// <summary>
    /// Working hours for a given date based on the work schedule that was valid on that date
    // (Most recent record where ValidFrom <= date). Requires WorkSchedule to be loaded.
    /// </summary>
    public decimal GetHoursFor(DateOnly date)
    {
        var entry = WorkSchedule
            .Where(w => w.ValidFrom <= date)
            .OrderByDescending(w => w.ValidFrom)
            .FirstOrDefault();

        return entry?.GetHours(date.DayOfWeek) ?? 0m;
    }
}