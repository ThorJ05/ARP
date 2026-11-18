using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using BusinessEsbjerg.Holiday.Service.Balances;
using BusinessEsbjerg.Holiday.Service.Calendar;

namespace BusinessEsbjerg.Holiday.Service.Requests;

public enum QueueKind { Holiday, Cancellation, Transfer }

// A request waiting for the accountant, with the employee's balance on BalanceDate (the last day of the
// holiday, earned hours and approved holiday included) and what it will be after an approval.
// For a transfer StartDate is the start of the period the hours come from.
public record QueueItem(
    Guid Id, QueueKind Kind, Guid EmployeeId, string EmployeeName, AccountType Account,
    DateOnly StartDate, DateOnly? EndDate, decimal Hours, string? Comment, DateTime CreatedAtUtc,
    DateOnly BalanceDate, decimal BalanceHours, decimal BalanceAfterHours);

// The accountant's side: the queue, and approving or declining requests. An approval writes new ledger
// entries, a declined request writes nothing. Old entries are never changed.
public class RequestReviewService(
    IHolidayRequestRepository holidays, ITransferRequestRepository transfers, IEmployeeRepository employees,
    ILedgerRepository ledger, BalanceService balances, DaysOffService daysOff, IClock clock)
{
    private static readonly RequestStatus[] Waiting = [RequestStatus.Pending, RequestStatus.CancellationRequested];

    public async Task<IReadOnlyList<QueueItem>> GetQueueAsync(CancellationToken ct = default)
    {
        var names = (await employees.GetAllAsync(ct)).ToDictionary(e => e.Employee.Id, e => e.Employee.Name);
        var holidayRequests = await holidays.GetByStatusAsync(Waiting, ct);
        var transferRequests = await transfers.GetByStatusAsync([RequestStatus.Pending], ct);

        var queue = new List<QueueItem>();
        foreach (var item in holidayRequests.Select(FromHoliday).Concat(transferRequests.Select(FromTransfer)))
            queue.Add(await WithBalanceAsync(item, names.GetValueOrDefault(item.EmployeeId, "?"), ct));

        return queue.OrderBy(item => item.CreatedAtUtc).ToList();
    }

    private static QueueItem FromHoliday(HolidayRequest r) => new(
        r.Id, r.Status == RequestStatus.Pending ? QueueKind.Holiday : QueueKind.Cancellation, r.EmployeeId, "",
        r.AccountType, r.StartDate, r.EndDate, r.Hours, r.Comment, r.CreatedAtUtc, r.EndDate, 0m, 0m);

    private static QueueItem FromTransfer(TransferRequest t) => new(
        t.Id, QueueKind.Transfer, t.EmployeeId, "", t.AccountType, t.FromPeriodStart, null, t.Hours, t.Comment,
        t.CreatedAtUtc, DateOnly.MinValue, 0m, 0m);

    private async Task<QueueItem> WithBalanceAsync(QueueItem item, string employeeName, CancellationToken ct)
    {
        var date = item.BalanceDate > clock.Today ? item.BalanceDate : clock.Today;
        var views = await balances.GetBalancesAsync(item.EmployeeId, date, ct) ?? [];
        var balance = views.FirstOrDefault(v => v.Balance.Type == item.Account)?.Balance.TotalHours ?? 0m;
        var after = item.Kind switch
        {
            QueueKind.Holiday => balance - item.Hours,
            QueueKind.Cancellation => balance + item.Hours,
            _ => balance // a transfer moves hours to the next period, the total stays the same
        };

        return item with
        {
            EmployeeName = employeeName, BalanceDate = date, BalanceHours = balance, BalanceAfterHours = after
        };
    }

    public async Task<ChangeResult> DecideHolidayAsync(Guid requestId, bool approve, string? reason, Guid userId,
        CancellationToken ct = default)
    {
        var request = await holidays.GetByIdAsync(requestId, ct);
        if (request is null) return ChangeResult.Missing;

        var cancellation = request.Status == RequestStatus.CancellationRequested;
        var error = approve
            ? request.Approve(userId, reason, clock.UtcNow)
            : request.Decline(userId, reason, clock.UtcNow);
        if (error is not null) return ChangeResult.Invalid(error);

        var entries = approve ? await LedgerEntriesAsync(request, cancellation, ct) : [];
        await holidays.SaveDecisionAsync(request, entries, ct);
        return ChangeResult.Ok;
    }

    public async Task<ChangeResult> DecideTransferAsync(Guid transferId, bool approve, string? reason, Guid userId,
        CancellationToken ct = default)
    {
        var transfer = await transfers.GetByIdAsync(transferId, ct);
        if (transfer is null) return ChangeResult.Missing;

        var error = approve
            ? transfer.Approve(userId, reason, clock.UtcNow)
            : transfer.Decline(userId, reason, clock.UtcNow);
        if (error is not null) return ChangeResult.Invalid(error);

        IReadOnlyList<LedgerEntry> entries = [];
        if (approve)
        {
            var accounts = await ledger.GetAccountsAsync(transfer.EmployeeId, ct);
            var account = accounts.Single(a => a.Type == transfer.AccountType);
            entries = RequestLedger.Transfer(transfer, account.Id, clock.Today);
        }

        await transfers.SaveDecisionAsync(transfer, entries, ct);
        return ChangeResult.Ok;
    }

    // approved holiday uses the hours of each working day, an approved cancellation gives them back
    private async Task<IReadOnlyList<LedgerEntry>> LedgerEntriesAsync(HolidayRequest request, bool cancellation,
        CancellationToken ct)
    {
        var data = await balances.GetUpToDateDataAsync(request.EmployeeId, ct)
            ?? throw new InvalidOperationException($"The employee of request {request.Id} doesn't exist.");

        if (cancellation)
            return RequestLedger.Cancellation(request, data.Entries);

        var account = data.Accounts.Single(a => a.Type == request.AccountType);
        var free = await daysOff.GetBetweenAsync(request.StartDate, request.EndDate, ct);
        return RequestLedger.Usage(request, account.Id,
            HolidayDays.Between(data.Employee, request.StartDate, request.EndDate, free));
    }
}
