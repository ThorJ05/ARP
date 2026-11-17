using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Repositories;

/// <summary>Holiday accounts and ledger entries, read with linq2db. The balance is the sum of the entries.</summary>
public class LedgerRepository(AppDbContext context) : ILedgerRepository
{
    public async Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(Guid employeeId, DateOnly? before = null,
        CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();

        var accounts = await db.GetTable<HolidayAccount>().Where(a => a.EmployeeId == employeeId).ToListAsync(ct);
        var accountIds = accounts.Select(a => a.Id).ToList();

        var entries = db.GetTable<LedgerEntry>().Where(e => accountIds.Contains(e.AccountId));
        if (before is { } day)
            entries = entries.Where(e => e.Date < day);

        return Combine(accounts, await SumPerAccountAsync(entries, ct));
    }

    public async Task<IReadOnlyList<AccountBalance>> GetAllBalancesAsync(CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();

        var accounts = await db.GetTable<HolidayAccount>().ToListAsync(ct);
        return Combine(accounts, await SumPerAccountAsync(db.GetTable<LedgerEntry>(), ct));
    }

    private static async Task<Dictionary<Guid, decimal>> SumPerAccountAsync(IQueryable<LedgerEntry> entries,
        CancellationToken ct)
    {
        var sums = await entries
            .GroupBy(e => e.AccountId)
            .Select(g => new { AccountId = g.Key, Hours = g.Sum(e => e.Hours) })
            .ToListAsync(ct);
        return sums.ToDictionary(s => s.AccountId, s => s.Hours);
    }

    // accounts without entries get a balance of 0
    private static List<AccountBalance> Combine(IEnumerable<HolidayAccount> accounts, Dictionary<Guid, decimal> sums) =>
        accounts
            .OrderBy(a => a.Type)
            .Select(a => new AccountBalance(a.EmployeeId, a.Id, a.Type, sums.GetValueOrDefault(a.Id)))
            .ToList();
}
