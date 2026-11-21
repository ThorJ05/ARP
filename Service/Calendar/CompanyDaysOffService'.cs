using BusinessEsbjerg.Holiday.Domain.Calendar;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Service.Abstractions;

namespace BusinessEsbjerg.Holiday.Service.Calendar;

// One day off in the calendar. Id is null for public holidays (they are calculated, not stored).
public record CalendarDay(Guid? Id, DateOnly Date, string Name, bool IsPublicHoliday);

// The system owner's company days off (e.g. 24 December). Public holidays come from the law and are only shown.
public class CompanyDaysOffService(ICompanyDayOffRepository companyDaysOff, IClock clock)
{
    // Public holidays and company days off in one year, sorted by date.
    public async Task<IReadOnlyList<CalendarDay>> GetYearAsync(int year, CancellationToken ct = default)
    {
        var company = await companyDaysOff.GetBetweenAsync(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31), ct);
        var publicHolidays = DanishPublicHolidays.In(year)
            .Select(d => new CalendarDay(null, d, "Public holiday", true));

        return company
            .Select(d => new CalendarDay(d.Id, d.Date, d.Name, false))
            .Concat(publicHolidays)
            .OrderBy(d => d.Date)
            .ToList();
    }

    public async Task<ChangeResult> AddAsync(DateOnly date, string name, CancellationToken ct = default)
    {
        // Only future dates, so holiday that is already planned or taken isn't changed.
        if (date <= clock.Today)
            return ChangeResult.Invalid("The day must be after today.");

        // The name must not be empty.
        if (string.IsNullOrWhiteSpace(name))
            return ChangeResult.Invalid("The name cannot be empty.");

        // It must not already be a public holiday.
        if (DanishPublicHolidays.In(date.Year).Contains(date))
            return ChangeResult.Invalid("That day is already a public holiday.");

        // There must not already be a company day off on that date.
        var sameDay = await companyDaysOff.GetBetweenAsync(date, date, ct);
        if (sameDay.Count > 0)          
            return ChangeResult.Invalid("There is already a company day off on that date.");

        await companyDaysOff.AddAsync(new CompanyDayOff { Date = date, Name = name.Trim() }, ct);
        return ChangeResult.Ok;
    }

    public async Task<ChangeResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // Only future days can be removed, so past calculations stay the same.
        var future = await companyDaysOff.GetBetweenAsync(clock.Today.AddDays(1), clock.Today.AddYears(10), ct);
        if (future.All(d => d.Id != id))
            return ChangeResult.Invalid("Only future company days off can be removed.");

        await companyDaysOff.DeleteAsync(id, ct);
        return ChangeResult.Ok;
    }
}