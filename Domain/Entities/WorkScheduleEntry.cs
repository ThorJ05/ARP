namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>
/// Weekly schedule (Monday-Friday) with a start and end time for each day. A day without times is a day off.
/// A schedule is used from ValidFrom until the next one starts, so when the hours change we add a new
/// entry instead of editing the old one. That way we keep the history.
/// Only accounting and the system owner can change schedules.
/// </summary>
public class WorkScheduleEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>The date the schedule starts.</summary>
    public DateOnly ValidFrom { get; set; }

    public TimeOnly? MondayStart { get; set; }
    public TimeOnly? MondayEnd { get; set; }
    public TimeOnly? TuesdayStart { get; set; }
    public TimeOnly? TuesdayEnd { get; set; }
    public TimeOnly? WednesdayStart { get; set; }
    public TimeOnly? WednesdayEnd { get; set; }
    public TimeOnly? ThursdayStart { get; set; }
    public TimeOnly? ThursdayEnd { get; set; }
    public TimeOnly? FridayStart { get; set; }
    public TimeOnly? FridayEnd { get; set; }

    private static readonly DayOfWeek[] WorkWeek =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    ];

    public decimal WeeklyHours => WorkWeek.Sum(day => GetHours(day));

    public int WorkingDays => WorkWeek.Count(day => GetHours(day) > 0m);

    /// <summary>Average hours per working day, e.g. 37 / 5 = 7.4 for full time. Used to convert between days and hours.</summary>
    public decimal AverageDayHours => WorkingDays == 0 ? 0m : Math.Round(WeeklyHours / WorkingDays, 2);

    /// <summary>Hours on a given weekday. Weekends always return 0.</summary>
    public decimal GetHours(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Duration(MondayStart, MondayEnd),
        DayOfWeek.Tuesday => Duration(TuesdayStart, TuesdayEnd),
        DayOfWeek.Wednesday => Duration(WednesdayStart, WednesdayEnd),
        DayOfWeek.Thursday => Duration(ThursdayStart, ThursdayEnd),
        DayOfWeek.Friday => Duration(FridayStart, FridayEnd),
        _ => 0m
    };

    /// <summary>
    /// Converts the balance when the schedule changes, so the employee keeps the same number of days.
    /// Example: 74 h on full time (7.4 h/day) is 10 days, which is 75 h on a 7.5 h/day part-time schedule.
    /// </summary>
    public static decimal ConvertBalance(decimal hours, WorkScheduleEntry from, WorkScheduleEntry to)
    {
        if (from.AverageDayHours == 0m)
            throw new InvalidOperationException("The old schedule has no working days, so the balance cannot be converted.");

        var days = hours / from.AverageDayHours;
        return Math.Round(days * to.AverageDayHours, 2, MidpointRounding.AwayFromZero);
    }

    // works like a constructor for our DbSeeder (Mon-Thu 7.5 h, Fri 7 h = 37 h)
    public static WorkScheduleEntry FullTime(Guid employeeId, DateOnly validFrom) => new()
    {
        EmployeeId = employeeId,
        ValidFrom = validFrom,
        MondayStart = new TimeOnly(8, 0), MondayEnd = new TimeOnly(15, 30),
        TuesdayStart = new TimeOnly(8, 0), TuesdayEnd = new TimeOnly(15, 30),
        WednesdayStart = new TimeOnly(8, 0), WednesdayEnd = new TimeOnly(15, 30),
        ThursdayStart = new TimeOnly(8, 0), ThursdayEnd = new TimeOnly(15, 30),
        FridayStart = new TimeOnly(8, 0), FridayEnd = new TimeOnly(15, 0)
    };

    private static decimal Duration(TimeOnly? start, TimeOnly? end) =>
        start is { } s && end is { } e && e > s ? Math.Round((decimal)(e - s).TotalHours, 2) : 0m;
}
