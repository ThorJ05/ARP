using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Data;

public record BootstrapLogin(Role Role, string? Email, string? Password);

// Creates the accountant and system owner logins on a new database (also in production), so someone can
// log in and create the other users. Emails and passwords come from configuration (fly secrets in production,
// never git). Nothing happens if a login with that role already exists.
public class AccountBootstrapper(AppDbContext context, IPasswordHasher hasher)
{
    // Returns a warning for each role that has no login and none configured.
    public async Task<IReadOnlyList<string>> EnsureAsync(IEnumerable<BootstrapLogin> logins)
    {
        await using var db = context.CreateLinqToDBConnection();
        var warnings = new List<string>();

        foreach (var login in logins)
        {
            if (await db.GetTable<User>().AnyAsync(u => u.Role == login.Role))
                continue;

            if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
                warnings.Add($"There is no {login.Role} login and none is configured (Bootstrap:{login.Role}:Email and Password).");
            else
                await db.InsertAsync(NewUser(login.Role, login.Email, login.Password));
        }

        return warnings;
    }

    private User NewUser(Role role, string email, string password)
    {
        var user = new User { Email = User.NormalizeEmail(email), Role = role };
        user.PasswordHash = hasher.Hash(user, password);
        return user;
    }
}
