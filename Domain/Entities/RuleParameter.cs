namespace BusinessEsbjerg.Holiday.Domain.Entities;


public class RuleParameter
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public DateOnly EffectiveFrom { get; set; }
    public string? Description { get; set; }
}

public static class RuleKeys
{
    public const string Weeks1To5DaysPerMonth = "weeks1to5.days_per_month";           // 2.08
    public const string Week6HoursPerMonth = "week6.hours_per_month";                 // 3.083
    public const string Week6HoursPerCalendarDay = "week6.hours_per_calendar_day";    // 0.103
    public const string Week6Enabled = "week6.enabled";                               // true
    public const string MainHolidayDays = "main_holiday.days";                        // 15
    public const string MainHolidayWindowStart = "main_holiday.window_start";         // 05-01
    public const string MainHolidayWindowEnd = "main_holiday.window_end";             // 09-30
    public const string NoticeMainHolidayMonths = "notice.main_holiday_months";       // 3
    public const string NoticeOtherHolidayMonths = "notice.other_holiday_months";     // 1
    public const string SupplementBasePercent = "supplement.base_percent";            // 1.00
    public const string SupplementRaisedPercent = "supplement.raised_percent";        // 0.95 -> 1.48
    public const string PayoutPercentWeeks1To5 = "payout.percent_weeks1to5";          // 12.5
    public const string PayoutPercentWeek6 = "payout.percent_week6";                  // 2.5
    public const string TransferDeadlineWeek5 = "transfer.deadline_week5";            // 12-31
    public const string TransferDeadlineWeek6 = "transfer.deadline_week6";            // 09-30
    public const string FullTimeWeeklyHours = "fulltime.weekly_hours";                // 37
}