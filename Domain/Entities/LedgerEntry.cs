using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

// One line in a holiday account, in hours. Positive = earned, negative = used, transferred or paid out.
// We never change old entries, corrections are added as new entries instead.
// No money is stored here, payroll takes care of that.
public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid AccountId { get; set; }
    public HolidayAccount Account { get; set; } = null!;

    public LedgerEntryType Type { get; set; }

    // Hours, positive or negative.
    public decimal Hours { get; set; }

    public DateOnly Date { get; set; }

    // Accrual: the month (its first day). Schedule conversion: the day the new schedule starts.
    // With the unique index this makes sure the same accrual or conversion is never saved twice.
    public DateOnly? PeriodMonth { get; set; }

    // When the earned hours expire (31 Dec / 30 Apr).
    public DateOnly? ExpiresOn { get; set; }

    public string? Reason { get; set; }

    // The holiday request this entry belongs to.
    public Guid? RequestId { get; set; }
    public HolidayRequest? Request { get; set; }

    // The transfer request this entry belongs to.
    public Guid? TransferRequestId { get; set; }
    public TransferRequest? TransferRequest { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
