using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using Service.Abstractions;

namespace Infrastructure.Repositories;

/// <summary>Employee data (balances and requests), read with linq2db.</summary>
public class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<Employee>().FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(Guid employeeId, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();

        var accounts = await db.GetTable<HolidayAccount>()
            .Where(a => a.EmployeeId == employeeId)
            .ToListAsync(ct);

        var accountIds = accounts.Select(a => a.Id).ToList();

        // the balance is the sum of all ledger entries on the account
        var sums = await db.GetTable<LedgerEntry>()
            .Where(e => accountIds.Contains(e.AccountId))
            .GroupBy(e => e.AccountId)
            .Select(g => new { AccountId = g.Key, Hours = g.Sum(e => e.Hours) })
            .ToListAsync(ct);

        return accounts
            .OrderBy(a => a.Type)
            .Select(a => new AccountBalance(a.Type, sums.FirstOrDefault(s => s.AccountId == a.Id)?.Hours ?? 0m))
            .ToList();
    }

    public async Task<IReadOnlyList<HolidayRequest>> GetRequestsAsync(Guid employeeId, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        
        return await db.GetTable<HolidayRequest>()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(ct);
    }
}
