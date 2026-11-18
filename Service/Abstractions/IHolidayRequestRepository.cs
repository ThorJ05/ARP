using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

public interface IHolidayRequestRepository
{
    Task<IReadOnlyList<HolidayRequest>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<HolidayRequest>> GetByStatusAsync(IReadOnlyCollection<RequestStatus> statuses,
        CancellationToken ct = default);
    Task<HolidayRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(HolidayRequest request, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, RequestStatus status, CancellationToken ct = default);

    // Saves the accountant's decision and the new ledger entries in one transaction.
    Task SaveDecisionAsync(HolidayRequest request, IReadOnlyCollection<LedgerEntry> entries,
        CancellationToken ct = default);
}
