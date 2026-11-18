using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Repositories;

// Transfer requests, read and written with linq2db.
public class TransferRequestRepository(AppDbContext context) : ITransferRequestRepository
{
    public async Task<IReadOnlyList<TransferRequest>> GetByStatusAsync(IReadOnlyCollection<RequestStatus> statuses,
        CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<TransferRequest>()
            .Where(t => statuses.Contains(t.Status))
            .OrderBy(t => t.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<TransferRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        return await db.GetTable<TransferRequest>().FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task SaveDecisionAsync(TransferRequest transfer, IReadOnlyCollection<LedgerEntry> entries,
        CancellationToken ct = default)
    {
        await using var db = context.CreateLinqToDBConnection();
        await using var transaction = await db.BeginTransactionAsync(ct);

        await db.GetTable<TransferRequest>()
            .Where(t => t.Id == transfer.Id)
            .Set(t => t.Status, transfer.Status)
            .Set(t => t.DecisionReason, transfer.DecisionReason)
            .Set(t => t.DecidedByUserId, transfer.DecidedByUserId)
            .Set(t => t.DecidedAtUtc, transfer.DecidedAtUtc)
            .UpdateAsync(ct);
        foreach (var entry in entries)
            await db.InsertAsync(entry, token: ct);

        await transaction.CommitAsync(ct);
    }
}
