using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using BusinessEsbjerg.Holiday.Service.Balances;

namespace BusinessEsbjerg.Holiday.Service.Requests;

// The result: either a list of errors, or the id of the saved request.
public record CreatedTransfer(IReadOnlyList<string> Errors, Guid? TransferId);

// The employee's own transfer requests. Approving and declining is done by the accountant.
public class TransferRequestService(
    ITransferRequestRepository transfers, BalanceService balances, IClock clock)
{
    public Task<IReadOnlyList<TransferRequest>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        transfers.GetForEmployeeAsync(employeeId, ct);

    // Saves the request as pending if it follows the rules. Null if the employee doesn't exist.
    public async Task<CreatedTransfer?> CreateAsync(Guid employeeId, NewTransferRequest request, string? comment,
        CancellationToken ct = default)
    {
        // 1. Get the employee's data (accounts, ledger entries, rules)
        var data = await balances.GetUpToDateDataAsync(employeeId, ct);
        if (data is null) return null;

        // 2. Get the employee's other transfer requests (for check 6)
        var others = await transfers.GetForEmployeeAsync(employeeId, ct);

        // 3. Run your rules
        var rules = new TransferRules(data.Employee, data.Accounts, data.Entries, others, data.Rules, clock.Today);
        var errors = rules.Check(request);
        if (errors.Count > 0) return new CreatedTransfer(errors, null);

        // 4. Build the new request, and save it
        var saved = new TransferRequest
        {
            EmployeeId =  employeeId,
            AccountType = request.Account,
            FromPeriodStart = request.FromPeriodStart,
            Hours = request.Hours,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
        };
        await transfers.AddAsync(saved, ct);

        return new CreatedTransfer([], saved.Id);
    }
}