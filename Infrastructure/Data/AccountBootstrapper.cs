using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using Infrastructure.Auth;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Data;

public record BootstrapLogin(Role Role, string? Email, string? Password);

/// <summary>
/// Creates the accountant and system owner logins on a new database (also in production), so someone can
/// log in and create the other users. Emails and passwords come from configuration (fly secrets in production,
/// never git). Nothing happens if a login with that role already exists.
/// </summary>
public static class AccountBootstrapper
{
    public static async Task<IReadOnlyList<string>> EnsureAsync(AppDbContext context, IEnumerable<BootstrapLogin> logins)
    {
        await using var db = context.CreateLinqToDBConnection();
        var hasher = new Pbkdf2PasswordHasher();
        var warnings = new List<string>();

        foreach (var login in logins)
        {
            if (await db.GetTable<User>().AnyAsync(u => u.Role == login.Role))
                continue;

            if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
            {
                warnings.Add($"There is no {login.Role} login and none is configured (Bootstrap:{login.Role}:Email and Password).");
                continue;
            }

            var user = new User { Email = User.NormalizeEmail(login.Email), Role = login.Role };
            user.PasswordHash = hasher.Hash(user, login.Password);
            await db.InsertAsync(user);
        }

        return warnings;
    }
}
