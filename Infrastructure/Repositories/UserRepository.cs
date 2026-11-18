using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Repositories;

// Logins, read and written with linq2db.
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
}
