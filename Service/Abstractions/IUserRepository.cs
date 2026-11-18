using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

// Logins.
public interface IUserRepository

{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    // Saves FaileddLoginCount, LockoutEndUtc and PasswordHash after a login attempt.
    Task UpdateLoginStateAsync(User user, CancellationToken ct = default);
}
