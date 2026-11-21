using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

public interface ICompanyDayOffRepository
{
    Task<IReadOnlyList<CompanyDayOff>> GetBetweenAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    
    Task AddAsync(CompanyDayOff companyDayOff, CancellationToken ct = default);
    
    //true if a day was deleted, false if it didn't exist
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
