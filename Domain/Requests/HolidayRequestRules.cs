using System.Globalization;
using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Rules;

namespace BusinessEsbjerg.Holiday.Domain.Requests;

public record NewHolidayRequest(AccountType Account, DateOnly Start, DateOnly End);

// The cost of a request and the rules it breaks (none means it can be sent).
public record RequestCheck(decimal Hours, int WorkingDays, IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

// Checks a holiday request against the rules before it is sent to the accountant.
// otherRequests are the employee's active requests, they count as used even before they are approved.
// daysOff are public holidays and company days off, they cost nothing.
public class HolidayRequestRules(
    Employee employee, IReadOnlyList<HolidayAccount> accounts, IReadOnlyList<LedgerEntry> entries,
    IReadOnlyList<HolidayRequest> otherRequests, RuleSet rules, IReadOnlySet<DateOnly> daysOff, DateOnly today)
{
    public RequestCheck Check(NewHolidayRequest request)
    {
        var dateError = CheckDates(request);
        if (dateError is not null)
            return new RequestCheck(0m, 0, [dateError]);

        var days = HolidayDays.Between(employee, request.Start, request.End, daysOff);
        if (days.Count == 0)
            return new RequestCheck(0m, 0, ["There are no working days in the chosen period."]);

        var errors = new List<string>();
        AddIfNotNull(errors, CheckOverlap(request));
        AddIfNotNull(errors, CheckNotice(request, days.Count));
        AddIfNotNull(errors, CheckMainHolidayWindow(request, days.Count));
        errors.AddRange(CheckBalance(request, days));

        return new RequestCheck(days.Sum(d => d.Hours), days.Count, errors);
    }

    // the date the holiday debt has to be paid back by (31 August) on or after the given date
    public static DateOnly SettleDateOnOrAfter(DateOnly date, RuleSet rules)
    {
        var settle = DateInYear(rules.Get(RuleKeys.HolidayDebtSettleDate, date), date.Year);
        return settle >= date ? settle : settle.AddYears(1);
    }

    private string? CheckDates(NewHolidayRequest request)
    {
        if (request.End < request.Start)
            return "The end date can't be before the start date.";
        if (request.End > request.Start.AddYears(1))
            return "A request can be at most one year long.";
        if (request.Start < employee.HireDate)
            return $"You can't take holiday before your first day ({Format(employee.HireDate)}).";
        if (employee.TerminationDate is { } lastDay && request.End > lastDay)
            return $"You can't take holiday after your last day ({Format(lastDay)}).";
        return null;
    }

    private string? CheckOverlap(NewHolidayRequest request) =>
        otherRequests.Any(r => r.Overlaps(request.Start, request.End))
            ? "You already have a request for some of these days."
            : null;

    private string? CheckNotice(NewHolidayRequest request, int workingDays)
    {
        var main = IsMainHoliday(request, workingDays);
        var noticeRule = main ? RuleKeys.NoticeMainHolidayMonths : RuleKeys.NoticeOtherHolidayMonths;
        var months = (int)rules.GetDecimal(noticeRule, today);
        var earliest = today.AddMonths(months);
        if (request.Start >= earliest) return null;

        var what = main ? "A main holiday" : "Holiday";
        var period = months == 1 ? "1 month" : $"{months} months";
        return $"{what} must be requested {period} ahead, so it can start on {Format(earliest)} at the earliest.";
    }

    private string? CheckMainHolidayWindow(NewHolidayRequest request, int workingDays)
    {
        if (!IsMainHoliday(request, workingDays)) return null;

        var (_, windowEnd) = MainHolidayWindow(request.Start);
        return request.End > windowEnd
            ? $"A main holiday has to be over by {Format(windowEnd)}."
            : null;
    }

    // weeks 1-5 holiday of at least 15 working days that starts between 1 May and 30 September
    private bool IsMainHoliday(NewHolidayRequest request, int workingDays)
    {
        if (request.Account != AccountType.Weeks1To5) return false;
        if (workingDays < rules.GetDecimal(RuleKeys.MainHolidayDays, request.Start)) return false;

        var (windowStart, windowEnd) = MainHolidayWindow(request.Start);
        return request.Start >= windowStart && request.Start <= windowEnd;
    }

    private (DateOnly Start, DateOnly End) MainHolidayWindow(DateOnly date) =>
        (DateInYear(rules.Get(RuleKeys.MainHolidayWindowStart, date), date.Year),
         DateInYear(rules.Get(RuleKeys.MainHolidayWindowEnd, date), date.Year));

    private IEnumerable<string> CheckBalance(NewHolidayRequest request, IReadOnlyList<HolidayDay> days)
    {
        var account = accounts.SingleOrDefault(a => a.Type == request.Account);
        if (account is null)
            return ["You don't have that holiday account."];

        var settleDate = SettleDateOnOrAfter(request.End, rules);
        var ledger = entries
            .Concat(AccrualPlanner.Plan(employee, accounts, entries, rules, settleDate))
            .Concat(OtherRequestsAsUsage())
            .Where(e => e.AccountId == account.Id)
            .ToList();
        var hours = days.Sum(d => d.Hours);

        if (request.Account == AccountType.Week6)
            return CheckWeek6(ledger, request.Start, hours);

        ledger.AddRange(days.Select(d => Usage(account.Id, d)));
        return CheckWeeks1To5(ledger, days, settleDate);
    }

    // week 6 can't be borrowed, it has to be earned and usable on the first day
    private IEnumerable<string> CheckWeek6(List<LedgerEntry> ledger, DateOnly start, decimal hours)
    {
        var usable = BalanceCalculator.Calculate(AccountType.Week6, ledger, start).AvailableHours;
        if (hours <= usable) return [];

        return [$"You can use {Format(usable)} hours of week 6 from {Format(start)}, but the request costs " +
                $"{Format(hours)} hours. Week 6 earned in a year can be used from 1 May the year after."];
    }

    // holiday debt up to 5 days is allowed, but the balance must be 0 or more on 31 August
    private IEnumerable<string> CheckWeeks1To5(List<LedgerEntry> ledger, IReadOnlyList<HolidayDay> days,
        DateOnly settleDate)
    {
        foreach (var day in days)
        {
            var balance = BalanceCalculator.Calculate(AccountType.Weeks1To5, ledger, day.Date).TotalHours;
            var maxDays = rules.GetDecimal(RuleKeys.HolidayDebtMaxDays, day.Date);
            var maxDebt = maxDays * (employee.GetScheduleFor(day.Date)?.AverageDayHours ?? 0m);
            if (balance < -maxDebt)
                return [$"The request would take your balance down to {Format(balance)} hours. " +
                        $"You can borrow at most {maxDays:0} days ({Format(maxDebt)} hours)."];
        }

        var atSettleDate = BalanceCalculator.Calculate(AccountType.Weeks1To5, ledger, settleDate).TotalHours;
        return atSettleDate < 0m
            ? [$"Your balance would be {Format(atSettleDate)} hours on {Format(settleDate)}. " +
               "Holiday debt has to be earned back by then."]
            : [];
    }

    // requests that are not in the ledger yet, one entry per working day
    private IEnumerable<LedgerEntry> OtherRequestsAsUsage()
    {
        var inLedger = entries.Where(e => e.RequestId is not null).Select(e => e.RequestId!.Value).ToHashSet();

        return from r in otherRequests
               where !inLedger.Contains(r.Id)
               let account = accounts.SingleOrDefault(a => a.Type == r.AccountType)
               where account is not null
               from day in HolidayDays.Between(employee, r.StartDate, r.EndDate, daysOff)
               select Usage(account!.Id, day);
    }

    private static LedgerEntry Usage(Guid accountId, HolidayDay day) =>
        new() { AccountId = accountId, Type = LedgerEntryType.Usage, Hours = -day.Hours, Date = day.Date };

    // rule values like "08-31"
    internal static DateOnly DateInYear(string monthDay, int year) =>
        DateOnly.ParseExact($"{year}-{monthDay}", "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string Format(decimal hours) => hours.ToString("0.00", CultureInfo.InvariantCulture);

    private static void AddIfNotNull(List<string> errors, string? error)
    {
        if (error is not null) errors.Add(error);
    }
}
