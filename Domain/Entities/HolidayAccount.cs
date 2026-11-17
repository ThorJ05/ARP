using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>A holiday account (weeks 1-5 or week 6). The balance is the sum of all entries.</summary>
public class HolidayAccount
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public AccountType Type { get; set; }

    public List<LedgerEntry> Entries { get; set; } = new();

    public decimal BalanceHours => Entries.Sum(e => e.Hours);

    public decimal BalanceHoursAt(DateOnly date) =>
        Entries.Where(e => e.Date <= date).Sum(e => e.Hours);
}