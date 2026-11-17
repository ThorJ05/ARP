using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace Service.Abstractions;

public record AccountBalance(Guid AccountId, AccountType Type, decimal Hours);

/// <summary>An employee with work schedules loaded, the login email and the balances.</summary>
public record EmployeeOverview(Employee Employee, string? Email, IReadOnlyList<AccountBalance> Balances);

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeOverview>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Balance per account. With "before" only entries dated before that day are counted.</summary>
    Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(Guid employeeId, DateOnly? before = null, CancellationToken ct = default);

    Task<IReadOnlyList<HolidayRequest>> GetRequestsAsync(Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkScheduleEntry>> GetSchedulesAsync(Guid employeeId, CancellationToken ct = default);

    /// <summary>Saves a new schedule and the balance conversions in one transaction.</summary>
    Task AddScheduleAsync(WorkScheduleEntry schedule, IEnumerable<LedgerEntry> conversions, CancellationToken ct = default);

    Task SetTerminationDateAsync(Guid employeeId, DateOnly? terminationDate, CancellationToken ct = default);
}
