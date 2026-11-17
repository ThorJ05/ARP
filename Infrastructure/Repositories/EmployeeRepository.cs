using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using Service.Abstractions;

namespace Infrastructure.Repositories;

/// <summary>Employees, schedules and balances, read and written with linq2db.</summary>
public class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<Employee>().FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IReadOnlyList<EmployeeOverview>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();

        // a few simple queries, put together in memory (the company has few employees)
        var employees = await db.GetTable<Employee>().OrderBy(e => e.Name).ToListAsync(ct);
        var schedules = await db.GetTable<WorkScheduleEntry>().ToListAsync(ct);
        var logins = await db.GetTable<User>()
            .Where(u => u.EmployeeId != null)
            .Select(u => new { u.EmployeeId, u.Email })
            .ToListAsync(ct);
        var accounts = await db.GetTable<HolidayAccount>().ToListAsync(ct);
        var sums = await db.GetTable<LedgerEntry>()
            .GroupBy(e => e.AccountId)
            .Select(g => new { AccountId = g.Key, Hours = g.Sum(e => e.Hours) })
            .ToListAsync(ct);

        return employees.Select(e =>
        {
            e.WorkSchedule = schedules.Where(s => s.EmployeeId == e.Id).ToList();
            var balances = accounts
                .Where(a => a.EmployeeId == e.Id)
                .OrderBy(a => a.Type)
                .Select(a => new AccountBalance(a.Id, a.Type, sums.FirstOrDefault(s => s.AccountId == a.Id)?.Hours ?? 0m))
                .ToList();
            return new EmployeeOverview(e, logins.FirstOrDefault(l => l.EmployeeId == e.Id)?.Email, balances);
        }).ToList();
    }

    public async Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(Guid employeeId, DateOnly? before = null,
        CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();

        var accounts = await db.GetTable<HolidayAccount>()
            .Where(a => a.EmployeeId == employeeId)
            .ToListAsync(ct);

        var accountIds = accounts.Select(a => a.Id).ToList();
        var entries = db.GetTable<LedgerEntry>().Where(e => accountIds.Contains(e.AccountId));
        if (before is { } day)
            entries = entries.Where(e => e.Date < day);

        // the balance is the sum of all ledger entries on the account
        var sums = await entries
            .GroupBy(e => e.AccountId)
            .Select(g => new { AccountId = g.Key, Hours = g.Sum(e => e.Hours) })
            .ToListAsync(ct);

        return accounts
            .OrderBy(a => a.Type)
            .Select(a => new AccountBalance(a.Id, a.Type, sums.FirstOrDefault(s => s.AccountId == a.Id)?.Hours ?? 0m))
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

    public async Task<IReadOnlyList<WorkScheduleEntry>> GetSchedulesAsync(Guid employeeId, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<WorkScheduleEntry>()
            .Where(s => s.EmployeeId == employeeId)
            .OrderBy(s => s.ValidFrom)
            .ToListAsync(ct);
    }

    public async Task AddScheduleAsync(WorkScheduleEntry schedule, IEnumerable<LedgerEntry> conversions,
        CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await using var transaction = await db.BeginTransactionAsync(ct);

        await db.InsertAsync(schedule, token: ct);
        foreach (var entry in conversions)
            await db.InsertAsync(entry, token: ct);

        await transaction.CommitAsync(ct);
    }

    public async Task SetTerminationDateAsync(Guid employeeId, DateOnly? terminationDate, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await db.GetTable<Employee>()
            .Where(e => e.Id == employeeId)
            .Set(e => e.TerminationDate, terminationDate)
            .UpdateAsync(ct);
    }
}
