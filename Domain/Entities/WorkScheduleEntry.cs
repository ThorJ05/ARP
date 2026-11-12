namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>
/// Work schedule with an effective-from date. When working hours change, a new record is added
/// to ensure that historical calculations remain accurate.
/// </summary>
public class WorkScheduleEntry
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly ValidFrom { get; set; }

    public decimal MonHours { get; set; }
    public decimal TueHours { get; set; }
    public decimal WedHours { get; set; }
    public decimal ThuHours { get; set; }
    public decimal FriHours { get; set; }
    public decimal SatHours { get; set; }
    public decimal SunHours { get; set; }

    public decimal WeeklyHours =>
        MonHours + TueHours + WedHours + ThuHours + FriHours + SatHours + SunHours;

    public decimal GetHours(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => MonHours,
        DayOfWeek.Tuesday => TueHours,
        DayOfWeek.Wednesday => WedHours,
        DayOfWeek.Thursday => ThuHours,
        DayOfWeek.Friday => FriHours,
        DayOfWeek.Saturday => SatHours,
        DayOfWeek.Sunday => SunHours,
        _ => 0m
    };

    
    public static WorkScheduleEntry FullTime(int employeeId, DateOnly validFrom) => new()
    {
        EmployeeId = employeeId,
        ValidFrom = validFrom,
        MonHours = 7.5m, TueHours = 7.5m, WedHours = 7.5m, ThuHours = 7.5m,
        FriHours = 7m
    };
}