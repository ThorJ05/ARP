using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Service.Abstractions;

namespace BusinessEsbjerg.Holiday.Service.Auth;

// FailReason is only for the log, the caller always gets the same message.
public record LoginResult(bool Succeeded, AuthToken? Token, string? FailReason)
{
    public static LoginResult Failed(string reason) => new(false, null, reason);
}

// Login with email and password. After too many failed logins in a row the account is locked for a while,
// and while it's locked we don't even check the password. The answer is the same for a wrong password,
// an unknown email and a locked account, so you can't use the login to find out which emails exist.
public class AuthService(
    IUserRepository users, IPasswordHasher hasher, ITokenService tokens, IClock clock, LockoutSettings lockout)
{
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(User.NormalizeEmail(email), ct);
        if (user is null)
            return FailWithoutCheck(password, "unknown email");

        if (user.IsLockedOut(clock.UtcNow))
            return FailWithoutCheck(password, "account is locked");

        var check = hasher.Verify(user, password);
        if (check == PasswordCheck.Failed)
            return await WrongPasswordAsync(user, ct);

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        if (check == PasswordCheck.SuccessRehashNeeded)
            user.PasswordHash = hasher.Hash(user, password); // upgrade an old hash now that we know the password

        await users.UpdateLoginStateAsync(user, ct);
        return new LoginResult(true, tokens.CreateToken(user), null);
    }

    // hash anyway, so an unknown email or a locked account takes as long as a wrong password
    private LoginResult FailWithoutCheck(string password, string reason)
    {
        hasher.Hash(new User(), password);
        return LoginResult.Failed(reason);
    }

    // counts the failed login and locks the account after too many in a row
    private async Task<LoginResult> WrongPasswordAsync(User user, CancellationToken ct)
    {
        user.FailedLoginCount++;
        var locked = user.FailedLoginCount >= lockout.MaxFailedAttempts;
        if (locked)
        {
            user.LockoutEndUtc = clock.UtcNow.AddMinutes(lockout.LockoutMinutes);
            user.FailedLoginCount = 0;
        }

        await users.UpdateLoginStateAsync(user, ct);
        return LoginResult.Failed(locked ? "wrong password, account locked" : "wrong password");
    }
}
