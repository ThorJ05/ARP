using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

// A holiday account (weeks 1-5 or week 6). The balance is calculated from the entries by BalanceCalculator.
public class HolidayAccount
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public AccountType Type { get; set; }

    public List<LedgerEntry> Entries { get; set; } = new();
}