using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>
/// Request to move unused holiday to the next period (max 5 days by law, needs a written agreement).
/// The accountant approves or rejects it, and the approval counts as the written agreement.
/// Business Esbjerg doesn't allow this right now (transfer.enabled = false), but it's built in case that changes.
/// </summary>
public class TransferRequest
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>Which balance the hours come from.</summary>
    public AccountType AccountType { get; set; }

    /// <summary>Start of the earning period the hours come from, e.g. 1 September 2025.</summary>
    public DateOnly FromPeriodStart { get; set; }

    /// <summary>Hours to transfer.</summary>
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
