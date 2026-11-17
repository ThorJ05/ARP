using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using Service.Abstractions;

namespace Service.Users;

/// <summary>Monday-Friday start and end times. A day without times is a day off.</summary>
public record WorkWeek(
    TimeOnly? MondayStart, TimeOnly? MondayEnd,
    TimeOnly? TuesdayStart, TimeOnly? TuesdayEnd,
    TimeOnly? WednesdayStart, TimeOnly? WednesdayEnd,
    TimeOnly? ThursdayStart, TimeOnly? ThursdayEnd,
    TimeOnly? FridayStart, TimeOnly? FridayEnd)
{
    public WorkScheduleEntry ToSchedule(Guid employeeId, DateOnly validFrom) => new()
    {
        EmployeeId = employeeId,
        ValidFrom = validFrom,
        MondayStart = MondayStart, MondayEnd = MondayEnd,
        TuesdayStart = TuesdayStart, TuesdayEnd = TuesdayEnd,
        WednesdayStart = WednesdayStart, WednesdayEnd = WednesdayEnd,
        ThursdayStart = ThursdayStart, ThursdayEnd = ThursdayEnd,
        FridayStart = FridayStart, FridayEnd = FridayEnd
    };

    public static WorkWeek From(WorkScheduleEntry s) => new(
        s.MondayStart, s.MondayEnd, s.TuesdayStart, s.TuesdayEnd, s.WednesdayStart, s.WednesdayEnd,
        s.ThursdayStart, s.ThursdayEnd, s.FridayStart, s.FridayEnd);
}

public record NewEmployee(string Name, string Email, string Password, DateOnly HireDate, WorkWeek Schedule);

public record CreateEmployeeResult(bool EmailTaken, Guid EmployeeId, Guid UserId);

/// <summary>Creates users. Only the system owner may do this (checked by the API).</summary>
public class UserService(IUserRepository users, IPasswordHasher hasher)
{
    /// <summary>
    /// Creates an employee with a login, a work schedule from the hire date and both holiday accounts.
    /// Every employee needs a schedule, otherwise we can't tell how many hours a day off costs.
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

        var user = new User { Email = email, Role = Role.Employee, EmployeeId = employee.Id };
        user.PasswordHash = hasher.Hash(user, input.Password);

        await users.CreateEmployeeAsync(employee, schedule, accounts, user, ct);
        return new CreateEmployeeResult(false, employee.Id, user.Id);
    }
}
