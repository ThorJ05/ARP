using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Balances;

// Hours with the same expiry date.
public record BalanceBucket(DateOnly AvailableFrom, DateOnly ExpiresOn, decimal Hours);

// The balance of one account on one date.
// Buckets are the hours that haven't expired yet, the ones that expire first come first.
// ExpiredHours expired without being used (they must be transferred or paid out).
public record AccountBalanceAt(AccountType Type, DateOnly Date, IReadOnlyList<BalanceBucket> Buckets, decimal ExpiredHours)
{
    public decimal TotalHours => Buckets.Sum(b => b.Hours);

    // What can be taken on the date (week 6 hours can't be used until 1 May the year after).
    public decimal AvailableHours => Buckets.Where(b => b.AvailableFrom <= Date).Sum(b => b.Hours);
}

// Calculates a balance from the ledger entries, split by expiry date.
// Used hours are taken from the holiday that expir es first.
public static class BalanceCalculator
{
    
    public static AccountBalanceAt Calculate(AccountType type, IEnumerable<LedgerEntry> entries, DateOnly date)
    {
        var buckets = new SortedDictionary<DateOnly, decimal>();

        var counted = WithoutCancelledRequests(entries).Where(e => e.Date <= date);
        foreach (var entry in counted.OrderBy(e => e.Date).ThenBy(Order))
            Apply(type, buckets, entry);

        var active = buckets
            .Where(b => b.Key >= date && b.Value != 0m)
            .Select(b => new BalanceBucket(HolidayPeriods.AvailableFrom(type, b.Key), b.Key, b.Value))
            .ToList();
        var expired = buckets.Where(b => b.Key < date && b.Value > 0m).Sum(b => b.Value);

        return new AccountBalanceAt(type, date, active, expired);
    }

    // A cancelled request has both its usage and the cancellation in the ledger. Le aving out both gives
    // the hours back to exactly the periods they were taken from.
    private static IEnumerable<LedgerEntry> WithoutCancelledRequests(IEnumerable<LedgerEntry> entries)
    {
        var list = entries.ToList();
        
        var cancelled = list
            .Where(e => e.Type == LedgerEntryType.Cancellation && e.RequestId is not null)
            .Select(e => e.RequestId!.Value)
            .ToHashSet();

        return list.Where(e => e.RequestId is not { } id || !cancelled.Contains(id));
    }

    // on the same day: a conversion first (it changes what was there before), then earned hours, then used hours
    private static int Order(LedgerEntry entry) =>
        entry.Type == LedgerEntryType.ScheduleConversion ? 0 : entry.Hours >= 0m ? 1 : 2;

    private static void Apply(AccountType type, SortedDictionary<DateOnly, decimal> buckets, LedgerEntry entry)
    {
        if (entry.Type == LedgerEntryType.ScheduleConversion)
            Distribute(type, buckets, entry);
        else if (entry.ExpiresOn is { } expiresOn)
            Add(buckets, expiresOn, entry.Hours);
        else if (entry.Hours >= 0m)
            
            Add(buckets, HolidayPeriods.ExpiresOn(type, entry.Date), entry.Hours);
        else
            Use(type, buckets, entry);
    }

    // used hours come from the holiday that expires first and can be used on that day
    private static void Use(AccountType type, SortedDictionary<DateOnly, decimal> buckets, LedgerEntry entry)
    {
        var left = -entry.Hours;
        var usable = buckets.Keys
            .Where(expiresOn => expiresOn >= entry.Date && HolidayPeriods.AvailableFrom(type, expiresOn) <= entry.Date)
            .ToList();

        foreach (var expiresOn in usable)
        {
            var take = Math.Min(left, Math.Max(buckets[expiresOn], 0m));
            buckets[expiresOn] -= take;
            left -= take;
            if (left == 0m) return;
            
        }

        // more than the balance: holiday debt in the current period
        Add(buckets, HolidayPeriods.ExpiresOn(type, entry.Date), -left);
    }

    // a schedule conversion changes every unexpired balance by the same factor
    private static void Distribute(AccountType type, SortedDictionary<DateOnly, decimal> buckets, LedgerEntry entry)
    {
        var targets = buckets.Where(b => b.Key >= entry.Date && b.Value > 0m).ToList();
        var total = targets.Sum(b => b.Value);
        if (total == 0m)
        {
            Add(buckets, HolidayPeriods.ExpiresOn(type, entry.Date), entry.Hours);
            return;
        }

        var left = entry.Hours;
        for (var i = 0; i < targets.Count; i++)
        {
            var part = i == targets.Count - 1
                ? left // the last one gets the rest, so the parts always add up after rounding
                : Math.Round(entry.Hours * targets[i].Value / total, 2, MidpointRounding.AwayFromZero);
            buckets[targets[i].Key] += part;
            left -= part;
        }
        
    }

    private static void Add(SortedDictionary<DateOnly, decimal> buckets, DateOnly expiresOn, decimal hours) =>
        buckets[expiresOn] = buckets.GetValueOrDefault(expiresOn) + hours;
}
