using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using Service.Abstractions;

namespace Infrastructure.Repositories;

/// <summary>Users and logins, read and written with linq2db.</summary>
public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<User>().FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<User>().FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<User>().AnyAsync(u => u.Email == email, ct);
    }

    public async Task UpdateLoginStateAsync(User user, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await db.GetTable<User>()
            .Where(u => u.Id == user.Id)
            .Set(u => u.FailedLoginCount, user.FailedLoginCount)
            .Set(u => u.LockoutEndUtc, user.LockoutEndUtc)
            .Set(u => u.PasswordHash, user.PasswordHash)
            .UpdateAsync(ct);
    }

    public async Task CreateEmployeeAsync(Employee employee, WorkScheduleEntry schedule,
        IEnumerable<HolidayAccount> accounts, User user, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await using var transaction = await db.BeginTransactionAsync(ct);

        await db.InsertAsync(employee, token: ct);
        await db.InsertAsync(schedule, token: ct);
        foreach (var account in accounts)
            await db.InsertAsync(account, token: ct);
        await db.InsertAsync(user, token: ct);

        await transaction.CommitAsync(ct);
    }
}
