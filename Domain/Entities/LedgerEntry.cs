using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>
/// One line in a holiday account, in hours. Positive = earned, negative = used, transferred or paid out.
/// We never change old entries, corrections are added as new entries instead.
/// No money is stored here, payroll takes care of that.
/// </summary>
public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid AccountId { get; set; }
    public HolidayAccount Account { get; set; } = null!;

    public LedgerEntryType Type { get; set; }

    /// <summary>Hours, positive or negative.</summary>
    public decimal Hours { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>
    /// For accrual entries only: the accrual month (represented by the first day of the month).
    /// Used to ensure idempotency.
    /// </summary>
    public DateOnly? PeriodMonth { get; set; }

    /// <summary>When the earned hours expire (31 Dec / 30 Apr).</summary>
    public DateOnly? ExpiresOn { get; set; }

    public string? Reason { get; set; }

    /// <summary>The holiday request this entry belongs to.</summary>
    public Guid? RequestId { get; set; }
    public HolidayRequest? Request { get; set; }

    /// <summary>The transfer request this entry belongs to.</summary>
    public Guid? TransferRequestId { get; set; }
    public TransferRequest? TransferRequest { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
