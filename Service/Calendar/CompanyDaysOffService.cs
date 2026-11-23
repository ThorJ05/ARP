using BusinessEsbjerg.Holiday.Domain.Calendar;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Service.Abstractions;

namespace BusinessEsbjerg.Holiday.Service.Calendar;

// One day off in the calendar. Id is null for public holidays (they are calculated, not stored).
public record CalendarDay(Guid? Id, DateOnly Date, string Name, bool IsPublicHoliday);

// The system owner's company days off (e.g. 24 December). Public holidays come from the law and are only shown.
// The checks are in Domain (CompanyDayOffRules), so they can be unit tested without a database.
public class CompanyDaysOffService(
    ICompanyDayOffRepository companyDaysOff, IHolidayRequestRepository holidays, IClock clock)
{
    // holiday whose hours are already worked out with the current days off
    private static readonly RequestStatus[] ActiveHoliday =
        [RequestStatus.Pending, RequestStatus.Approved, RequestStatus.CancellationRequested];

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
        var sameDay = await companyDaysOff.GetBetweenAsync(date, date, ct);
        var active = await holidays.GetByStatusAsync(ActiveHoliday, ct);

        var errors = CompanyDayOffRules.CheckAdd(date, name, clock.Today, sameDay, active);
        if (errors.Count > 0) return ChangeResult.Invalid(string.Join(" ", errors));

        await companyDaysOff.AddAsync(new CompanyDayOff { Date = date, Name = name.Trim() }, ct);
        return ChangeResult.Ok;
    }

    public async Task<ChangeResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // only future days can be found here, so past calculations stay the same
        var future = await companyDaysOff.GetBetweenAsync(clock.Today.AddDays(1), clock.Today.AddYears(10), ct);
        var day = future.FirstOrDefault(d => d.Id == id);
        var active = await holidays.GetByStatusAsync(ActiveHoliday, ct);

        var errors = CompanyDayOffRules.CheckRemove(day, clock.Today, active);
        if (errors.Count > 0) return ChangeResult.Invalid(string.Join(" ", errors));

        await companyDaysOff.DeleteAsync(id, ct);
        return ChangeResult.Ok;
    }
}
