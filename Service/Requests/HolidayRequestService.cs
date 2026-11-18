using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Requests;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using BusinessEsbjerg.Holiday.Service.Balances;
using BusinessEsbjerg.Holiday.Service.Calendar;

namespace BusinessEsbjerg.Holiday.Service.Requests;

public record RequestView(HolidayRequest Request, bool CanCancel);

public record CreatedRequest(RequestCheck Check, Guid? RequestId);

// The employee's own holiday requests. Approving and rejecting is done by the accountant.
public class HolidayRequestService(
    IHolidayRequestRepository requests, DaysOffService daysOff, BalanceService balances, IClock clock)
{
    public async Task<IReadOnlyList<RequestView>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        var mine = await requests.GetForEmployeeAsync(employeeId, ct);
        return mine.Select(r => new RequestView(r, r.CanBeCancelled(clock.Today))).ToList();
    }

    // The cost and the broken rules, without saving anything. Null if the employee doesn't exist.
    public async Task<RequestCheck?> PreviewAsync(Guid employeeId, NewHolidayRequest request,
        CancellationToken ct = default) =>
        (await RulesForAsync(employeeId, request, ct))?.Check(request);

    // Saves the request as pending if it follows the rules. Null if the employee doesn't exist.
    public async Task<CreatedRequest?> CreateAsync(Guid employeeId, NewHolidayRequest request, string? comment,
        CancellationToken ct = default)
    {
        var rules = await RulesForAsync(employeeId, request, ct);
        if (rules is null) return null;

        var check = rules.Check(request);
        if (!check.IsValid) return new CreatedRequest(check, null);

        var saved = new HolidayRequest
        {
            EmployeeId = employeeId,
            AccountType = request.Account,
            StartDate = request.Start,
            EndDate = request.End,
            Hours = check.Hours,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
        };
        await requests.AddAsync(saved, ct);
        return new CreatedRequest(check, saved.Id);
    }

    public async Task<ChangeResult> CancelAsync(Guid employeeId, Guid requestId, CancellationToken ct = default)
    {
        var request = await requests.GetByIdAsync(requestId, ct);
        if (request is null || request.EmployeeId != employeeId) return ChangeResult.Missing;

        var error = request.Cancel(clock.Today);
        if (error is not null) return ChangeResult.Invalid(error);

        await requests.UpdateStatusAsync(request.Id, request.Status, ct);
        return ChangeResult.Ok;
    }

    private async Task<HolidayRequestRules?> RulesForAsync(Guid employeeId, NewHolidayRequest request,
        CancellationToken ct)
    {
        var data = await balances.GetUpToDateDataAsync(employeeId, ct);
        if (data is null) return null;

        var others = (await requests.GetForEmployeeAsync(employeeId, ct)).Where(r => r.IsActive()).ToList();
        var lastDay = request.End > request.Start ? request.End : request.Start;
        var until = HolidayRequestRules.SettleDateOnOrAfter(lastDay, data.Rules);
        var free = await daysOff.GetBetweenAsync(clock.Today.AddYears(-1), until, ct);

        return new HolidayRequestRules(
            data.Employee, data.Accounts, data.Entries, others, data.Rules, free, clock.Today);
    }
}
