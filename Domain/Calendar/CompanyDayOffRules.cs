using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Domain.Calendar;

// Checks before a company day off is added or removed. The hours of holiday that is waiting or approved
// were worked out with the days off that existed then. Adding or removing a day off inside that holiday
// would make the employee pay for a day off (or get a day for free), so it isn't allowed.
public static class CompanyDayOffRules
{
    private const string CoveredByHoliday =
        "Approved or waiting holiday covers that date. Its hours were worked out without this change, " +
        "so that holiday must be cancelled or moved first.";

    public static IReadOnlyList<string> CheckAdd(DateOnly date, string name, DateOnly today,
        IReadOnlyCollection<CompanyDayOff> daysOffOnDate, IEnumerable<HolidayRequest> holidays)
    {
        var errors = new List<string>();

        // only future dates, so holiday that is already planned or taken isn't changed
        if (date <= today)
            errors.Add("The day must be after today.");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add("The name cannot be empty.");
        if (DanishPublicHolidays.In(date.Year).Contains(date))
            errors.Add("That day is already a public holiday.");
        if (daysOffOnDate.Count > 0)
            errors.Add("There is already a company day off on that date.");
        if (holidays.Any(h => h.Overlaps(date, date)))
            errors.Add(CoveredByHoliday);

        return errors;
    }

    // day is null when it doesn't exist or isn't in the future.
    public static IReadOnlyList<string> CheckRemove(CompanyDayOff? day, DateOnly today,
        IEnumerable<HolidayRequest> holidays)
    {
        if (day is null || day.Date <= today)
            return ["Only future company days off can be removed."];

        if (holidays.Any(h => h.Overlaps(day.Date, day.Date)))
            return [CoveredByHoliday];

        return [];
    }
}
