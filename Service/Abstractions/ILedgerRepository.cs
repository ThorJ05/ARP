using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Service.Abstractions;

public record AccountBalance(Guid EmployeeId, Guid AccountId, AccountType Type, decimal Hours);

/// <summary>Holiday accounts and their ledger entries (the balance is the sum of the entries).</summary>
public interface ILedgerRepository
{
    /// <summary>Balance per account for one employee. With "before" only entries dated before that day count.</summary>
    Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(Guid employeeId, DateOnly? before = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<AccountBalance>> GetAllBalancesAsync(CancellationToken ct = default);
}
