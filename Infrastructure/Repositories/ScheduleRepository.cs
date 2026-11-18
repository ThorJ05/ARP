using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Repositories;

/// <summary>Work schedules, read and written with linq2db.</summary>
public class ScheduleRepository(AppDbContext context) : IScheduleRepository
{
    public async Task<IReadOnlyList<WorkScheduleEntry>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<WorkScheduleEntry>()
            .Where(s => s.EmployeeId == employeeId)
            .OrderBy(s => s.ValidFrom)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WorkScheduleEntry>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<WorkScheduleEntry>().ToListAsync(ct);
    }

    public async Task AddAsync(WorkScheduleEntry schedule, IEnumerable<LedgerEntry> conversions,
        CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await using var transaction = await db.BeginTransactionAsync(ct);

        await db.InsertAsync(schedule, token: ct);
        foreach (var entry in conversions)
            await db.InsertAsync(entry, token: ct);

        await transaction.CommitAsync(ct);
    }
}
