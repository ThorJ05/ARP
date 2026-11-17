using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Service.Abstractions;

namespace BusinessEsbjerg.Holiday.Service.Requests;

/// <summary>Holiday requests. Creating, approving and cancelling requests will be added here.</summary>
public class HolidayRequestService(IHolidayRequestRepository requests)
{
    public Task<IReadOnlyList<HolidayRequest>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        requests.GetForEmployeeAsync(employeeId, ct);
}
