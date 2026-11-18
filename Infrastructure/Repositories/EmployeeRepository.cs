using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Repositories;

// Employees, read and written with linq2db.
public class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<Employee>().FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IReadOnlyList<EmployeeWithEmail>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();

        var employees = await db.GetTable<Employee>().OrderBy(e => e.Name).ToListAsync(ct);
        var emails = await db.GetTable<User>()
            .Where(u => u.EmployeeId != null)
            .Select(u => new { u.EmployeeId, u.Email })
            .ToListAsync(ct);

        return employees
            .Select(e => new EmployeeWithEmail(e, emails.FirstOrDefault(u => u.EmployeeId == e.Id)?.Email))
            .ToList();
    }

    public async Task CreateAsync(Employee employee, WorkScheduleEntry schedule, IEnumerable<HolidayAccount> accounts,
        User login, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await using var transaction = await db.BeginTransactionAsync(ct);

        await db.InsertAsync(employee, token: ct);
        await db.InsertAsync(schedule, token: ct);
        foreach (var account in accounts)
            await db.InsertAsync(account, token: ct);
        await db.InsertAsync(login, token: ct);

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
