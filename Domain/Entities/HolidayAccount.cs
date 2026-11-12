using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;


public class HolidayAccount
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public AccountType Type { get; set; }

    public List<LedgerEntry> Entries { get; set; } = new();

    public decimal BalanceHours => Entries.Sum(e => e.Hours);

    public decimal BalanceHoursAt(DateOnly date) =>
        Entries.Where(e => e.Date <= date).Sum(e => e.Hours);
}