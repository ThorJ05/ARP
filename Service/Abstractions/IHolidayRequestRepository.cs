using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

/// <summary>Holiday requests.</summary>
public interface IHolidayRequestRepository
{
    Task<IReadOnlyList<HolidayRequest>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
}
