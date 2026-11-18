using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace Service.Abstractions;

public record AccountBalance(AccountType Type, decimal Hours);

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<HolidayRequest>> GetRequestsAsync(Guid employeeId, CancellationToken ct = default);
}
