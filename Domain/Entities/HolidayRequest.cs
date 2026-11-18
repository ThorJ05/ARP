using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

// Holiday request in whole days that the accountant approves or rejects.
// If approved holiday hasn't been used yet, the employee has to ask for it to be cancelled (CancellationRequested).
public class HolidayRequest
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // Which balance to use: weeks 1-5 or week 6.
    public AccountType AccountType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    // Hours taken from the balance, based on the employee's work schedule.
    public decimal Hours { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Comment { get; set; }

    // Why the accountant rejected it.
    public string? DecisionReason { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public User? DecidedBy { get; set; }
    public DateTime? DecidedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // waiting, approved or waiting to be cancelled: the days are taken
    public bool IsActive() =>
        Status is RequestStatus.Pending or RequestStatus.Approved or RequestStatus.CancellationRequested;

    public bool Overlaps(DateOnly start, DateOnly end) =>
        IsActive() && StartDate <= end && start <= EndDate;

    // a pending request can be withdrawn, approved holiday only before it starts
    public bool CanBeCancelled(DateOnly today) =>
        Status == RequestStatus.Pending || (Status == RequestStatus.Approved && StartDate > today);

    // Pending is cancelled right away. Approved holiday has to be cancelled by the accountant,
    // so it becomes CancellationRequested. Returns an error if it can't be cancelled.
    public string? Cancel(DateOnly today)
    {
        if (!CanBeCancelled(today))
            return Status == RequestStatus.Approved
                ? "Holiday that has started can't be cancelled."
                : $"A request that is {Status} can't be cancelled.";

        Status = Status == RequestStatus.Pending ? RequestStatus.Cancelled : RequestStatus.CancellationRequested;
        return null;
    }
}
