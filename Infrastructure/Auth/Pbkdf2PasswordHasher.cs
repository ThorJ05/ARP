using BusinessEsbjerg.Holiday.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Service.Abstractions;

namespace Infrastructure.Auth;

/// <summary>
/// Salted PBKDF2 (HMAC-SHA512) using ASP.NET Core's PasswordHasher. Each password gets its own random salt,
/// which is stored in the hash together with the settings. 210,000 iterations as OWASP recommends.
/// Older hashes with fewer iterations still work and get upgraded the next time the user logs in.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    public const int Iterations = 210_000;

    private readonly PasswordHasher<User> _hasher =
        new(Options.Create(new PasswordHasherOptions { IterationCount = Iterations }));

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public PasswordCheck Verify(User user, string password) =>
        _hasher.VerifyHashedPassword(user, user.PasswordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheck.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
            _ => PasswordCheck.Failed
        };
}
