using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Rules;


namespace BusinessEsbjerg.Holiday.Domain.Requests;

// What the employee asks for: which balance, which holiday period, and how many hours.
public record NewTransferRequest(AccountType Account, DateOnly FromPeriodStart, Decimal Hours);

// Checks a transfer request against the holiday rules before it is sent to the accountant. Returns the broken rules (empty = OK).
public class TransferRules(Employee employee, IReadOnlyList<HolidayAccount> accounts, IReadOnlyList<LedgerEntry> entries, 
    IReadOnlyList<TransferRequest> otherTransfers, RuleSet rules, DateOnly today)
{
    public IReadOnlyList<string> Check(NewTransferRequest request)
    {
        var errors = new List<string>();
        // The system owner can switch transfers on/off with a rule, so stop here if it is off.
        if (!rules.GetBool(RuleKeys.TransferEnabled, today))
        {
            errors.Add("Rules not enabled");
            return errors;
        }
        
        if (request.Hours <= 0)
            errors.Add("You must transfer more than 0 hours");
        
        // The max is set in days, so convert it to hours with the employee's average workday.
        var maxDays = rules.GetDecimal(RuleKeys.TransferMaxDays, today);
        var dayHours = employee.GetScheduleFor(today)?.AverageDayHours ?? 0m;
        var maxHours = maxDays * dayHours;
        if (request.Hours > maxHours)
            errors.Add($"You can transfer at most {maxDays}, days ({maxHours} hours)");
        
        // You can't transfer more hours than are left in that holiday period.
        var account = accounts.Single(a => a.Type == request.Account);
        var accountEntries = entries.Where(e => e.AccountId == account.Id);
        var balance = BalanceCalculator.Calculate(request.Account, accountEntries, today);
        var expiresOn = HolidayPeriods.ExpiresOn(request.Account, request.FromPeriodStart);
        var hoursInPeriod = balance.Buckets.FirstOrDefault(b => b.ExpiresOn == expiresOn)?.Hours ?? 0;
        if (request.Hours >  hoursInPeriod)
            errors.Add($"There are only {hoursInPeriod} hours left in that period.");

        // Weeks 1-5 and week 6 have different deadlines, both read from the rules.
        var deadlineKey = request.Account switch
        {
            AccountType.Weeks1To5 => RuleKeys.TransferDeadlineWeek5, // the rule key for weeks 1-5
            AccountType.Week6 => RuleKeys.TransferDeadlineWeek6, // the rule key for week 6
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
        var deadlineYear = request.Account == AccountType.Weeks1To5 ? expiresOn.Year : expiresOn.Year - 1;
        var deadline = HolidayRequestRules.DateInYear(rules.Get(deadlineKey, today), deadlineYear);
        if (today > deadline)
            errors.Add($"The deadline for this was {deadline:dd-MM-yyyy}.");
        
        // Only one pending transfer per period, so the same hours can't be requested twice.
        if (otherTransfers.Any(t => t.AccountType == request.Account
                                                    && t.FromPeriodStart == request.FromPeriodStart
                                                    && t.Status == RequestStatus.Pending))
            errors.Add("You already have a transfer request waiting for this period.");

        return errors;
    }
}