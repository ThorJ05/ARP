using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using BusinessEsbjerg.Holiday.Service.Schedules;

namespace BusinessEsbjerg.Holiday.Service.Users;

public record NewEmployee(string Name, string Email, string Password, DateOnly HireDate, WorkWeek Schedule);

public record CreateEmployeeResult(bool EmailTaken, Guid EmployeeId, Guid UserId);

public record CurrentUser(Guid UserId, string Email, Role Role, Guid? EmployeeId, string? EmployeeName);

/// <summary>Logins: creating employees with a login, and who is logged in.</summary>
public class UserService(IUserRepository users, IEmployeeRepository employees, IPasswordHasher hasher)
{
    /// <summary>
    /// Creates an employee with a login, a work schedule from the hire date and both holiday accounts.
    /// Every employee needs a schedule, otherwise we can't tell how many hours a day off costs.
    /// Only the system owner may do this (checked by the API).
    /// </summary>
    public async Task<CreateEmployeeResult> CreateEmployeeAsync(NewEmployee input, CancellationToken ct = default)
    {
        var email = User.NormalizeEmail(input.Email);
        if (await users.EmailExistsAsync(email, ct))
            return new CreateEmployeeResult(true, Guid.Empty, Guid.Empty);

        var employee = new Employee { Name = input.Name.Trim(), HireDate = input.HireDate };
        var schedule = input.Schedule.ToSchedule(employee.Id, input.HireDate);
        var accounts = new[]
        {
            new HolidayAccount { EmployeeId = employee.Id, Type = AccountType.Weeks1To5 },
            new HolidayAccount { EmployeeId = employee.Id, Type = AccountType.Week6 }
        };

        var login = new User { Email = email, Role = Role.Employee, EmployeeId = employee.Id };
        login.PasswordHash = hasher.Hash(login, input.Password);

        await employees.CreateAsync(employee, schedule, accounts, login, ct);
        return new CreateEmployeeResult(false, employee.Id, login.Id);
    }

    /// <summary>The logged-in user (null if the login was deleted after the token was made).</summary>
    public async Task<CurrentUser?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return null;

        var employee = user.EmployeeId is { } id ? await employees.GetByIdAsync(id, ct) : null;
        return new CurrentUser(user.Id, user.Email, user.Role, user.EmployeeId, employee?.Name);
    }
}
