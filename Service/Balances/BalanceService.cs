using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Rules;
using BusinessEsbjerg.Holiday.Service.Abstractions;

namespace BusinessEsbjerg.Holiday.Service.Balances;

// A balance on a date, with the day length used to turn hours into days.
// What the holiday rules need about one employee, with the ledger up to date.
public record HolidayData(
    Employee Employee, IReadOnlyList<HolidayAccount> Accounts, IReadOnlyList<LedgerEntry> Entries, RuleSet Rules);

public record BalanceView(AccountBalanceAt Balance, decimal DayHours)
{
    public decimal ToDays(decimal hours) =>
        DayHours == 0m ? 0m : Math.Round(hours / DayHours, 2, MidpointRounding.AwayFromZero);
}

// Holiday balances. Accrual is saved in the ledger month by month. We don't have a background job
// (the fly machines stop when nobody uses them), so missing months are saved when a balance is read.
// A balance on a future date adds the months up to that date without saving them.
public class BalanceService(
    IEmployeeRepository employees, IScheduleRepository schedules, ILedgerRepository ledger, IRuleRepository rules,
    IClock clock)
{
    public const int MaxYearsAhead = 3;

    public bool IsTooFarAhead(DateOnly date) => date > clock.Today.AddYears(MaxYearsAhead);

    // One balance per account on the date (today if null). Null if the employee doesn't exist.
    public async Task<IReadOnlyList<BalanceView>?> GetBalancesAsync(Guid employeeId, DateOnly? date = null,
        CancellationToken ct = default)
    {
        var data = await CatchUpAndLoadAsync(employeeId, ct);
        var employee = data.Employees.SingleOrDefault();
        if (employee is null) return null;

        var day = date ?? clock.Today;
        var accounts = data.AccountsOf(employee.Id);
        IReadOnlyList<LedgerEntry> entries = data.EntriesOf(employee.Id);

        // the future months are only calculated, not saved
        if (day > clock.Today)
            entries = [..entries, ..AccrualPlanner.Plan(employee, accounts, entries, data.Rules, day)];

        var dayHours = (employee.GetScheduleFor(day) ?? employee.WorkSchedule.MinBy(s => s.ValidFrom))?.AverageDayHours ?? 0m;

        return accounts
            .OrderBy(a => a.Type)
            .Select(a => new BalanceView(
                BalanceCalculator.Calculate(a.Type, entries.Where(e => e.AccountId == a.Id), day), dayHours))
            .ToList();
    }

    // Today's balance in hours for every employee and account (for the employee list).
    public async Task<IReadOnlyDictionary<(Guid EmployeeId, AccountType Type), decimal>> GetAllCurrentHoursAsync(
        CancellationToken ct = default)
    {
        var data = await CatchUpAndLoadAsync(null, ct);
        var entriesByAccount = data.Entries.ToLookup(e => e.AccountId);

        return data.Accounts.ToDictionary(
            a => (a.EmployeeId, a.Type),
            a => BalanceCalculator.Calculate(a.Type, entriesByAccount[a.Id], clock.Today).TotalHours);
    }

    // Expired hours for every employee and account (for the accountant's report).
    public async Task<IReadOnlyDictionary<(Guid EmployeeId, AccountType Type), decimal>> GetAllExpiredHoursAsync(
        CancellationToken ct = default)
    {
        var data = await CatchUpAndLoadAsync(null, ct);
        var entriesByAccount = data.Entries.ToLookup(e => e.AccountId);

        return data.Accounts.ToDictionary(
            a => (a.EmployeeId, a.Type),
            a => BalanceCalculator.Calculate(a.Type, entriesByAccount[a.Id], clock.Today).ExpiredHours);
    }

    // The last day holiday has been saved for (missing months are saved first). A new schedule or end
    // date before this day would change holiday that is already saved, so that isn't allowed.
    public async Task<DateOnly?> GetLastAccrualDateAsync(Guid employeeId, CancellationToken ct = default)
    {
        var data = await CatchUpAndLoadAsync(employeeId, ct);
        return data.Entries
            .Where(e => e.Type == LedgerEntryType.Accrual)
            .Select(e => (DateOnly?)e.Date)
            .Max();
    }

    // Null if the employee doesn't exist.
    public async Task<HolidayData?> GetUpToDateDataAsync(Guid employeeId, CancellationToken ct = default)
    {
        var data = await CatchUpAndLoadAsync(employeeId, ct);
        var employee = data.Employees.SingleOrDefault();
        return employee is null
            ? null
            : new HolidayData(employee, data.AccountsOf(employee.Id), data.EntriesOf(employee.Id), data.Rules);
    }

    // Saves the accruals and conversions that are missing up to today.
    public Task CatchUpAsync(Guid employeeId, CancellationToken ct = default) => CatchUpAndLoadAsync(employeeId, ct);

    // employeeId null = everyone
    private async Task<LedgerData> CatchUpAndLoadAsync(Guid? employeeId, CancellationToken ct)
    {
        var data = await LoadAsync(employeeId, ct);

        var missing = data.Employees
            .SelectMany(e => AccrualPlanner.Plan(e, data.AccountsOf(e.Id), data.EntriesOf(e.Id), data.Rules, clock.Today))
            .ToList();
        if (missing.Count == 0) return data;

        // if another request saved the same months at the same time, read what it saved
        return await ledger.AddEntriesAsync(missing, ct)
            ? data with { Entries = [..data.Entries, ..missing] }
            : await LoadAsync(employeeId, ct);
    }

    private async Task<LedgerData> LoadAsync(Guid? employeeId, CancellationToken ct)
    {
        List<Employee> list;
        IReadOnlyList<WorkScheduleEntry> allSchedules;

        if (employeeId is { } id)
        {
            var employee = await employees.GetByIdAsync(id, ct);
            list = employee is null ? [] : [employee];
            allSchedules = await schedules.GetForEmployeeAsync(id, ct);
        }
        else
        {
            list = (await employees.GetAllAsync(ct)).Select(e => e.Employee).ToList();
            allSchedules = await schedules.GetAllAsync(ct);
        }

        var schedulesByEmployee = allSchedules.ToLookup(s => s.EmployeeId);
        foreach (var employee in list)
            employee.WorkSchedule = schedulesByEmployee[employee.Id].ToList();

        return new LedgerData(
            list,
            await ledger.GetAccountsAsync(employeeId, ct),
            await ledger.GetEntriesAsync(employeeId, ct),
            new RuleSet(await rules.GetAllAsync(ct)));
    }

    // everything needed to calculate balances, read once
    private record LedgerData(
        IReadOnlyList<Employee> Employees, IReadOnlyList<HolidayAccount> Accounts, IReadOnlyList<LedgerEntry> Entries,
        RuleSet Rules)
    {
        public IReadOnlyList<HolidayAccount> AccountsOf(Guid employeeId) =>
            Accounts.Where(a => a.EmployeeId == employeeId).ToList();

        public IReadOnlyList<LedgerEntry> EntriesOf(Guid employeeId)
        {
            var accountIds = AccountsOf(employeeId).Select(a => a.Id).ToHashSet();
            return Entries.Where(e => accountIds.Contains(e.AccountId)).ToList();
        }
    }
}
