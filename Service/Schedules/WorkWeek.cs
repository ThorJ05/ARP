using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Schedules;

// Monday-Friday start and end times. A day without times is a day off.
public record WorkWeek(
    TimeOnly? MondayStart, TimeOnly? MondayEnd,
    TimeOnly? TuesdayStart, TimeOnly? TuesdayEnd,
    TimeOnly? WednesdayStart, TimeOnly? WednesdayEnd,
    TimeOnly? ThursdayStart, TimeOnly? ThursdayEnd,
    TimeOnly? FridayStart, TimeOnly? FridayEnd)
{
    public WorkScheduleEntry ToSchedule(Guid employeeId, DateOnly validFrom) => new()
    {
        EmployeeId = employeeId,
        ValidFrom = validFrom,
        MondayStart = MondayStart, MondayEnd = MondayEnd,
        TuesdayStart = TuesdayStart, TuesdayEnd = TuesdayEnd,
        WednesdayStart = WednesdayStart, WednesdayEnd = WednesdayEnd,
        ThursdayStart = ThursdayStart, ThursdayEnd = ThursdayEnd,
        FridayStart = FridayStart, FridayEnd = FridayEnd
    };

    public static WorkWeek From(WorkScheduleEntry s) => new(
        s.MondayStart, s.MondayEnd, s.TuesdayStart, s.TuesdayEnd, s.WednesdayStart, s.WednesdayEnd,
        s.ThursdayStart, s.ThursdayEnd, s.FridayStart, s.FridayEnd);
}
