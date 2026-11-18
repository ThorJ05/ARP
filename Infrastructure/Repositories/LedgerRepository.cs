using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using Npgsql;

namespace BusinessEsbjerg.Holiday.Infrastructure.Repositories;

// Holiday accounts and ledger entries, read and written with linq2db.
public class LedgerRepository(AppDbContext context) : ILedgerRepository
{
    public async Task<IReadOnlyList<HolidayAccount>> GetAccountsAsync(Guid? employeeId = null,
        CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<HolidayAccount>()
            .Where(a => employeeId == null || a.EmployeeId == employeeId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LedgerEntry>> GetEntriesAsync(Guid? employeeId = null,
        CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();

        var accountIds = db.GetTable<HolidayAccount>()
            .Where(a => employeeId == null || a.EmployeeId == employeeId)
            .Select(a => a.Id);

        return await db.GetTable<LedgerEntry>()
            .Where(e => accountIds.Contains(e.AccountId))
            .OrderBy(e => e.Date)
            .ToListAsync(ct);
    }

    public async Task<bool> AddEntriesAsync(IReadOnlyCollection<LedgerEntry> entries, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await using var transaction = await db.BeginTransactionAsync(ct);

        try
        {
            foreach (var entry in entries)
                await db.InsertAsync(entry, token: ct);

            await transaction.CommitAsync(ct);
            return true;
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // another request saved the same accrual first
            await transaction.RollbackAsync(ct);
            return false;
        }
    }
}
