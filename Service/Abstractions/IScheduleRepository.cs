using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

/// <summary>Work schedules.</summary>
public interface IScheduleRepository
{
    Task<IReadOnlyList<WorkScheduleEntry>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkScheduleEntry>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Saves a new schedule and the balance conversions it causes in one transaction.</summary>
    Task AddAsync(WorkScheduleEntry schedule, IEnumerable<LedgerEntry> conversions, CancellationToken ct = default);
}
