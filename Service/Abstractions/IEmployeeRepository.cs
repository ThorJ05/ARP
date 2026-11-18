using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

/// <summary>An employee and the email of their login.</summary>
public record EmployeeWithEmail(Employee Employee, string? Email);

/// <summary>Employees.</summary>
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeWithEmail>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Saves a new employee with work schedule, holiday accounts and login in one transaction.</summary>
    Task CreateAsync(Employee employee, WorkScheduleEntry schedule, IEnumerable<HolidayAccount> accounts, User login,
        CancellationToken ct = default);

    Task SetTerminationDateAsync(Guid employeeId, DateOnly? terminationDate, CancellationToken ct = default);
}
