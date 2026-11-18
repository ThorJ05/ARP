using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

// Work schedules.
public interface IScheduleRepository
{
    Task<IReadOnlyList<WorkScheduleEntry>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkScheduleEntry>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(WorkScheduleEntry schedule, CancellationToken ct = default);
}
