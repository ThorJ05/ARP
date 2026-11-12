using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

public class HolidayRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public AccountType AccountType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Hours that is substracted form the worker</summary>
    public decimal Hours { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Comment { get; set; }

    /// <summary>reasun for decline</summary>
    public string? DecisionReason { get; set; }
    public int? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}