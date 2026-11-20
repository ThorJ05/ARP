using System.Globalization;
using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Rules;

namespace BusinessEsbjerg.Holiday.Domain.Requests;

// What the employee asks for: which balance, a date in the holiday period, and how many hours.
public record NewTransferRequest(AccountType Account, DateOnly FromPeriodStart, decimal Hours);

// Checks a transfer request against the holiday rules before it is sent to the accountant.
// Returns the broken rules (empty means it can be sent).
public class TransferRules(
    Employee employee, IReadOnlyList<HolidayAccount> accounts, IReadOnlyList<LedgerEntry> entries,
    IReadOnlyList<TransferRequest> otherTransfers, RuleSet rules, DateOnly today)
{
    public IReadOnlyList<string> Check(NewTransferRequest request)
    {
        // the system owner can switch transfers on and off with a rule
        if (!rules.GetBool(RuleKeys.TransferEnabled, today))
            return ["Transferring holiday to the next period is not allowed right now."];

        var account = accounts.SingleOrDefault(a => a.Type == request.Account);
        if (account is null)
            return ["You don't have that holiday account."];
        if (request.Hours <= 0m)
            return ["You must transfer more than 0 hours."];

        var expiresOn = HolidayPeriods.ExpiresOn(request.Account, request.FromPeriodStart);
        return new[]
            {
                CheckMax(request, expiresOn),
                CheckHoursLeft(request, account, expiresOn),
                CheckDeadline(request, expiresOn),
                CheckDuplicate(request, expiresOn)
            }
            .OfType<string>()
            .ToList();
    }

    // at most 5 days in total from a period, so earlier transfers from the same period count too
    private string? CheckMax(NewTransferRequest request, DateOnly expiresOn)
    {
        var maxDays = rules.GetDecimal(RuleKeys.TransferMaxDays, today);
        var maxHours = maxDays * (employee.GetScheduleFor(today)?.AverageDayHours ?? 0m);
        var alreadyAsked = SamePeriod(request.Account, expiresOn)
            .Where(t => t.Status is RequestStatus.Pending or RequestStatus.Approved)
            .Sum(t => t.Hours);
        if (alreadyAsked + request.Hours <= maxHours) return null;

        return $"You can transfer at most {maxDays:0} days ({Format(maxHours)} hours) from a period, " +
               $"and {Format(alreadyAsked)} hours from this period are already requested or transferred.";
    }

    private string? CheckHoursLeft(NewTransferRequest request, HolidayAccount account, DateOnly expiresOn)
    {
        var accountEntries = entries.Where(e => e.AccountId == account.Id);
        var balance = BalanceCalculator.Calculate(request.Account, accountEntries, today);
        var left = balance.Buckets.FirstOrDefault(b => b.ExpiresOn == expiresOn)?.Hours ?? 0m;
        return request.Hours > left ? $"There are only {Format(left)} hours left in that period." : null;
    }

    // weeks 1-5 and week 6 have different deadlines, both read from the rules
    private string? CheckDeadline(NewTransferRequest request, DateOnly expiresOn)
    {
        var (key, year) = request.Account == AccountType.Weeks1To5
            ? (RuleKeys.TransferDeadlineWeek5, expiresOn.Year)
            : (RuleKeys.TransferDeadlineWeek6, expiresOn.Year - 1);
        var deadline = HolidayRequestRules.DateInYear(rules.Get(key, today), year);
        return today > deadline ? $"The deadline for transferring from this period was {Format(deadline)}." : null;
    }

    // one waiting request per period, so the same hours can't be asked for twice
    private string? CheckDuplicate(NewTransferRequest request, DateOnly expiresOn) =>
        SamePeriod(request.Account, expiresOn).Any(t => t.Status == RequestStatus.Pending)
            ? "You already have a transfer request waiting for this period."
            : null;

    // compared on the expiry date, so any date in the same period counts as the same period
    private IEnumerable<TransferRequest> SamePeriod(AccountType account, DateOnly expiresOn) =>
        otherTransfers.Where(t =>
            t.AccountType == account && HolidayPeriods.ExpiresOn(account, t.FromPeriodStart) == expiresOn);

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string Format(decimal hours) => hours.ToString("0.00", CultureInfo.InvariantCulture);
}
