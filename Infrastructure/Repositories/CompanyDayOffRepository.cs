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
}
