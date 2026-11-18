using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

// An employee and the email of their login.
public record EmployeeWithEmail(Employee Employee, string? Email);

// Employees.
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeWithEmail>> GetAllAsync(CancellationToken ct = default);

    // Saves a new employee with work schedule, holiday accounts and login in one transaction.
    Task CreateAsync(Employee employee, WorkScheduleEntry schedule, IEnumerable<HolidayAccount> accounts, User login,
        CancellationToken ct = default);

    Task SetTerminationDateAsync(Guid employeeId, DateOnly? terminationDate, CancellationToken ct = default);
}
