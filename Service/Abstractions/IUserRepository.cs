using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

/// <summary>Logins.</summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>Saves FailedLoginCount, LockoutEndUtc and PasswordHash after a login attempt.</summary>
    Task UpdateLoginStateAsync(User user, CancellationToken ct = default);
}
