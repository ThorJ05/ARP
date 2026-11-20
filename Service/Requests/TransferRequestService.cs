using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Requests;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using BusinessEsbjerg.Holiday.Service.Balances;

namespace BusinessEsbjerg.Holiday.Service.Requests;

// Either the broken rules, or the id of the saved request.
public record CreatedTransfer(IReadOnlyList<string> Errors, Guid? TransferId);

// The employee's own transfer requests. Approving and declining is done by the accountant.
public class TransferRequestService(ITransferRequestRepository transfers, BalanceService balances, IClock clock)
{
    public Task<IReadOnlyList<TransferRequest>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        transfers.GetForEmployeeAsync(employeeId, ct);

    // Saves the request as pending if it follows the rules. Null if the employee doesn't exist.
    public async Task<CreatedTransfer?> CreateAsync(Guid employeeId, NewTransferRequest request, string? comment,
        CancellationToken ct = default)
    {
        var data = await balances.GetUpToDateDataAsync(employeeId, ct);
        if (data is null) return null;

        var others = await transfers.GetForEmployeeAsync(employeeId, ct);
        var rules = new TransferRules(data.Employee, data.Accounts, data.Entries, others, data.Rules, clock.Today);
        var errors = rules.Check(request);
        if (errors.Count > 0) return new CreatedTransfer(errors, null);

        var saved = new TransferRequest
        {
            EmployeeId = employeeId,
            AccountType = request.Account,
            // saved as the first day of the period, whatever day in the period the employee picked
            FromPeriodStart = HolidayPeriods.PeriodStart(request.Account, request.FromPeriodStart),
            Hours = request.Hours,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
        };
        await transfers.AddAsync(saved, ct);
        return new CreatedTransfer([], saved.Id);
    }
}
