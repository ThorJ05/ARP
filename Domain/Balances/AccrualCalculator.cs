using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Rules;

namespace BusinessEsbjerg.Holiday.Domain.Balances;

// What one employee earns in one month, in hours with two decimals.
// Month is the first day of the month. EarnedOn is the day the holiday counts as earned:
// the last day of the month, or the last day of employment.
public record MonthlyAccrual(DateOnly Month, DateOnly EarnedOn, decimal Weeks1To5Hours, decimal Week6Hours);

// Monthly accrual for weeks 1-5 and week 6. We go through the month day by day, so a start or end
// mid-month, a new schedule mid-month and a rule change are all handled the same way.
public static class AccrualCalculator
{
    // Returns null if the employee wasn't employed in that month. The employee's WorkSchedule must be loaded.
    public static MonthlyAccrual? ForMonth(Employee employee, DateOnly month, RuleSet rules)
    {
        var first = new DateOnly(month.Year, month.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var from = employee.HireDate > first ? employee.HireDate : first;
        var to = employee.TerminationDate is { } end && end < last ? end : last;
        if (from > to) return null;

        var daysInMonth = last.Day;
        var wholeMonth = from == first && to == last;
        decimal weeks1To5 = 0m, week6 = 0m;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var schedule = employee.GetScheduleFor(day);
            if (schedule is null) continue;

            // weeks 1-5: 2.08 days a month, turned into hours with the employee's own day length
            weeks1To5 += rules.GetDecimal(RuleKeys.Weeks1To5DaysPerMonth, day) / daysInMonth * schedule.AverageDayHours;

            if (!rules.GetBool(RuleKeys.Week6Enabled, day)) continue;

            // week 6: 3.083 hours a month for full time (0.103 per calendar day in a partial month), part time in proportion
            var share = schedule.WeeklyHours / rules.GetDecimal(RuleKeys.FullTimeWeeklyHours, day);
            var perDay = wholeMonth
                ? rules.GetDecimal(RuleKeys.Week6HoursPerMonth, day) / daysInMonth
                : rules.GetDecimal(RuleKeys.Week6HoursPerCalendarDay, day);
            week6 += share * perDay;
        }

        return new MonthlyAccrual(first, to, Round(weeks1To5), Round(week6));
    }

    private static decimal Round(decimal hours) => Math.Round(hours, 2, MidpointRounding.AwayFromZero);
}
