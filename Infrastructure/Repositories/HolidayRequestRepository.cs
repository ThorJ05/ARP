using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Repositories;

/// <summary>Holiday requests, read with linq2db.</summary>
public class HolidayRequestRepository(AppDbContext context) : IHolidayRequestRepository
{
    public async Task<IReadOnlyList<HolidayRequest>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<HolidayRequest>()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(ct);
    }
}
