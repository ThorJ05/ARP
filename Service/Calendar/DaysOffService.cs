using BusinessEsbjerg.Holiday.Domain.Calendar;
using BusinessEsbjerg.Holiday.Service.Abstractions;

namespace BusinessEsbjerg.Holiday.Service.Calendar;

// Days that cost no holiday: Danish public holidays and the company's own days off.
public class DaysOffService(ICompanyDayOffRepository companyDaysOff)
{
    public async Task<IReadOnlySet<DateOnly>> GetBetweenAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var days = (await companyDaysOff.GetBetweenAsync(from, to, ct)).Select(d => d.Date).ToHashSet();
        for (var year = from.Year; year <= to.Year; year++)
            days.UnionWith(DanishPublicHolidays.In(year));
        return days;
    }
}
