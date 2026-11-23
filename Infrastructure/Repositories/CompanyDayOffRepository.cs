using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Repositories;

public class CompanyDayOffRepository(AppDbContext context) : ICompanyDayOffRepository
{
    public async Task<IReadOnlyList<CompanyDayOff>> GetBetweenAsync(DateOnly from, DateOnly to,
        CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<CompanyDayOff>()
            .Where(d => d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .ToListAsync(ct);
    }
    // a new company day off, e.g. 24 december
    public async Task AddAsync(CompanyDayOff companyDayOff, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await db.InsertAsync( companyDayOff, token: ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        var deleted = await db.GetTable<CompanyDayOff>()
            .Where(d => d.Id == id)
            .DeleteAsync(ct);
        return deleted > 0;
    }
}
