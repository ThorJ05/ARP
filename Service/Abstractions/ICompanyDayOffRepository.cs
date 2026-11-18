using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

public interface ICompanyDayOffRepository
{
    Task<IReadOnlyList<CompanyDayOff>> GetBetweenAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
