using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>
///  Positive hours = accrual, negative hours = usage/payout.
/// Corrections and cancellations  are recorded as new entries with the opposite siggn ,
/// never by modifying existing entries.
/// </summary>
public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid AccountId { get; set; }
    public HolidayAccount Account { get; set; } = null!;

    public LedgerEntryType Type { get; set; }

    /// <summary> hourss </summary>
    public decimal Hours { get; set; }

   
    public DateOnly Date { get; set; }

    /// <summary>/// For accrual entries only: the accrual month (represented by the first day of the month).
    /// </summary>
    public DateOnly? PeriodMonth { get; set; }

    /// <summary>when the earned hours expire (31. dec / 30. apr).</summary>
    public DateOnly? ExpiresOn { get; set; }

    public string? Reason { get; set; }

    public Guid? RequestId { get; set; }
    public HolidayRequest? Request { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}