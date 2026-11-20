using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using BusinessEsbjerg.Holiday.Service.Balances;

namespace BusinessEsbjerg.Holiday.Service.Requests;

public record ApprovedHoliday(
    Guid Id, string EmployeeName, AccountType Account, DateOnly StartDate, DateOnly EndDate, decimal Hours);

// Change is the result; HoursGivenBack is how many hours the employee got back.
public record IllnessResult(ChangeResult Change, decimal HoursGivenBack);

// The accountant registers illness during approved holiday, and the sick days are given back.
public class IllnessService(
    IHolidayRequestRepository holidays, IEmployeeRepository employees, ILedgerRepository ledger,
    BalanceService balances)
{
    // Approved holiday for everyone, newest first, so the accountant can pick the one the illness belongs to.
    public async Task<IReadOnlyList<ApprovedHoliday>> GetApprovedHolidayAsync(CancellationToken ct = default)
    {
        var names = (await employees.GetAllAsync(ct)).ToDictionary(e => e.Employee.Id, e => e.Employee.Name);
        var approved = await holidays.GetByStatusAsync([RequestStatus.Approved], ct);

        return approved
            .OrderByDescending(r => r.StartDate)
            .Select(r => new ApprovedHoliday(r.Id, names.GetValueOrDefault(r.EmployeeId, "?"), r.AccountType,
                r.StartDate, r.EndDate, r.Hours))
            .ToList();
    }

    public async Task<IllnessResult> RegisterAsync(Guid requestId, DateOnly firstSickDay, DateOnly lastSickDay,
        CancellationToken ct = default)
    {
        var request = await holidays.GetByIdAsync(requestId, ct);
        if (request is null) return new IllnessResult(ChangeResult.Missing, 0m);

        var data = await balances.GetUpToDateDataAsync(request.EmployeeId, ct);
        if (data is null) return new IllnessResult(ChangeResult.Missing, 0m);

        // the rules are in Domain, so they can be unit tested without a database
        var illness = new HolidayIllness(request, data.Entries, data.Rules);
        var errors = illness.Check(firstSickDay, lastSickDay);
        if (errors.Count > 0) return new IllnessResult(ChangeResult.Invalid(string.Join(" ", errors)), 0m);

        // new ledger entries give the hours back, old entries are never changed
        var entries = RequestLedger.Illness(request, illness.DaysToGiveBack(firstSickDay, lastSickDay));
        await ledger.AddEntriesAsync(entries, ct);
        return new IllnessResult(ChangeResult.Ok, entries.Sum(e => e.Hours));
    }
}
