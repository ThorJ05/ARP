using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

public interface ITransferRequestRepository
{
    Task<IReadOnlyList<TransferRequest>> GetByStatusAsync(IReadOnlyCollection<RequestStatus> statuses,
        CancellationToken ct = default);
    Task<TransferRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    // Used for the employee's own list and to find duplicate pending requests.
    Task<IReadOnlyList<TransferRequest>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task AddAsync(TransferRequest request, CancellationToken ct = default);
    

    // Saves the accountant's decision and the new ledger entries in one transaction.
    Task SaveDecisionAsync(TransferRequest transfer, IReadOnlyCollection<LedgerEntry> entries,
        CancellationToken ct = default);
}
