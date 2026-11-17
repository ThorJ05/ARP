using BusinessEsbjerg.Holiday.Domain.Entities;
using Service.Abstractions;

namespace Service.Auth;

/// <summary>FailReason is only for the log, the caller always gets the same message.</summary>
public record LoginResult(bool Succeeded, AuthToken? Token, string? FailReason)
{
    public static LoginResult Failed(string reason) => new(false, null, reason);
}

/// <summary>
/// Login with email and password. After too many failed logins in a row the account is locked for a while,
/// and while it's locked we don't even check the password. The answer is the same for a wrong password,
/// an unknown email and a locked account, so you can't use the login to find out which emails exist.
/// </summary>
public class AuthService(
    IUserRepository users, IPasswordHasher hasher, ITokenService tokens, IClock clock, LockoutSettings lockout)
{
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(User.NormalizeEmail(email), ct);
        if (user is null)
        {
            // hash anyway so an unknown email takes as long as a wrong password
            hasher.Hash(new User(), password);
            return LoginResult.Failed("unknown email");
        }

        var now = clock.UtcNow;
        if (user.IsLockedOut(now))
            return LoginResult.Failed("account is locked");

        var check = hasher.Verify(user, password);
        if (check == PasswordCheck.Failed)
        {
            user.FailedLoginCount++;
            var locked = user.FailedLoginCount >= lockout.MaxFailedAttempts;
            if (locked)
            {
                user.LockoutEndUtc = now.AddMinutes(lockout.LockoutMinutes);
                user.FailedLoginCount = 0;
            }

            await users.UpdateLoginStateAsync(user, ct);
            return LoginResult.Failed(locked ? "wrong password, account locked" : "wrong password");
        }

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        if (check == PasswordCheck.SuccessRehashNeeded)
            user.PasswordHash = hasher.Hash(user, password); // upgrade an old hash now that we know the password

        await users.UpdateLoginStateAsync(user, ct);
        return new LoginResult(true, tokens.CreateToken(user), null);
    }
}
