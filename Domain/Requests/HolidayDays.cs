using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Domain.Requests;

public record HolidayDay(DateOnly Date, decimal Hours);

public static class HolidayDays
{
    // The working days from start to end (both included) and what each one costs: the hours of that
    // weekday in the schedule used on the day. Weekends and daysOff (public holidays, company days off) are free.
    public static IReadOnlyList<HolidayDay> Between(Employee employee, DateOnly start, DateOnly end,
        IReadOnlySet<DateOnly> daysOff)
    {
        var days = new List<HolidayDay>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var hours = daysOff.Contains(date) ? 0m : employee.GetHoursFor(date);
            if (hours > 0m)
                days.Add(new HolidayDay(date, hours));
        }

        return days;
    }
}
