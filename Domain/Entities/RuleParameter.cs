namespace BusinessEsbjerg.Holiday.Domain.Entities;

// A rule value with a start date. When a value changes we add a new row with a later date,
// so old calculations still use the old value.
public class RuleParameter
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public DateOnly EffectiveFrom { get; set; }
    public string? Description { get; set; }
}

public static class RuleKeys
{
    public const string Weeks1To5DaysPerMonth = "weeks1to5.days_per_month";           // 2.08
    public const string Week6Enabled = "week6.enabled";                               // true
    public const string Week6HoursPerMonth = "week6.hours_per_month";                 // 3.083
    public const string Week6HoursPerCalendarDay = "week6.hours_per_calendar_day";    // 0.103
    public const string FullTimeWeeklyHours = "fulltime.weekly_hours";                // 37
    public const string MainHolidayDays = "main_holiday.days";                        // 15
    public const string MainHolidayWindowStart = "main_holiday.window_start";         // 05-01
    public const string MainHolidayWindowEnd = "main_holiday.window_end";             // 09-30
    public const string NoticeMainHolidayMonths = "notice.main_holiday_months";       // 3
    public const string NoticeOtherHolidayMonths = "notice.other_holiday_months";     // 1
    public const string TransferEnabled = "transfer.enabled";                         // false
    public const string TransferMaxDays = "transfer.max_days";                        // 5
    public const string TransferDeadlineWeek5 = "transfer.deadline_week5";            // 12-31
    public const string TransferDeadlineWeek6 = "transfer.deadline_week6";            // 09-30
    public const string HolidayDebtMaxDays = "holiday_debt.max_days";                 // 5
    public const string HolidayDebtSettleDate = "holiday_debt.settle_date";           // 08-31
}
