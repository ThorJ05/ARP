using BusinessEsbjerg.Holiday.Domain.Entities;

namespace Service.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>Saves FailedLoginCount, LockoutEndUtc and PasswordHash after a login attempt.</summary>
    Task UpdateLoginStateAsync(User user, CancellationToken ct = default);

    /// <summary>Saves a new employee with schedule, holiday accounts and login in one transaction.</summary>
    Task CreateEmployeeAsync(Employee employee, WorkScheduleEntry schedule, IEnumerable<HolidayAccount> accounts,
        User user, CancellationToken ct = default);
}
