using BusinessEsbjerg.Holiday.Domain.Entities;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

// Holiday accounts and their ledger entries (the balance is calculated from the entries).
public interface ILedgerRepository
{
    // The accounts of one employee, or of everyone when employeeId is null.
    Task<IReadOnlyList<HolidayAccount>> GetAccountsAsync(Guid? employeeId = null, CancellationToken ct = default);

    // The ledger entries of one employee, or of everyone when employeeId is null.
    
    Task<IReadOnlyList<LedgerEntry>> GetEntriesAsync(Guid? employeeId = null, CancellationToken ct = default);

    // Saves new entries in one transaction. If another request saved the same accrual at the same time,
    // nothing is saved and false is returned (the unique index stops duplicates).
    Task<bool> AddEntriesAsync(IReadOnlyCollection<LedgerEntry> entries, CancellationToken ct = default);
}
