using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

public interface ITransferRequestRepository
{
    Task<IReadOnlyList<TransferRequest>> GetByStatusAsync(IReadOnlyCollection<RequestStatus> statuses,
        CancellationToken ct = default);
    Task<TransferRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    

    // Saves the accountant's decision and the new ledger e ntries in one transaction.
    Task SaveDecisionAsync(TransferRequest transfer, IReadOnlyCollection<LedgerEntry> entries,
        CancellationToken ct = default);
}
