using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>
/// Holiday request in whole days that the accountant approves or rejects.
/// If approved holiday hasn't been used yet, the employee has to ask for it to be cancelled (CancellationRequested).
/// </summary>
public class HolidayRequest
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>Which balance to use: weeks 1-5 or week 6.</summary>
    public AccountType AccountType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Hours taken from the balance, based on the employee's work schedule.</summary>
    public decimal Hours { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Comment { get; set; }

    /// <summary>Why the accountant rejected it.</summary>
    public string? DecisionReason { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public User? DecidedBy { get; set; }
    public DateTime? DecidedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
