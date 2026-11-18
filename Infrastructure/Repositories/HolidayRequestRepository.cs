using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Repositories;

// Holiday requests, read and written with linq2db.
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

    public async Task<HolidayRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<HolidayRequest>().FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task AddAsync(HolidayRequest request, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await db.InsertAsync(request, token: ct);
    }

    public async Task UpdateStatusAsync(Guid id, RequestStatus status, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await db.GetTable<HolidayRequest>()
            .Where(r => r.Id == id)
            .Set(r => r.Status, status)
            .UpdateAsync(ct);
    }
}
