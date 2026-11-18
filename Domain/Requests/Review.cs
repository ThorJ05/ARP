using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Requests;

// The decision fields that holiday and transfer requests both have.
public interface IReviewedRequest
{
    RequestStatus Status { get; set; }
    string? DecisionReason { get; set; }
    Guid? DecidedByUserId { get; set; }
    DateTime? DecidedAtUtc { get; set; }
}

public static class Review
{
    public const string ReasonMissing = "Write a reason, so the employee knows why.";

    public static void Record(IReviewedRequest request, RequestStatus status, Guid userId, string? reason, DateTime now)
    {
        request.Status = status;
        request.DecisionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        request.DecidedByUserId = userId;
        request.DecidedAtUtc = now;
    }
}
