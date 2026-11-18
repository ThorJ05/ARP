using System.Security.Claims;
using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Api.Validation;
using BusinessEsbjerg.Holiday.Service.Requests;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

// Kind is "Holiday", "Cancellation" or "Transfer". For a transfer StartDate is the start of the period.
// BalanceHours is the balance on BalanceDate, BalanceAfterHours what it will be if the request is approved.
public record QueueItemResponse(
    Guid Id, string Kind, Guid EmployeeId, string EmployeeName, string Account, DateOnly StartDate,
    DateOnly? EndDate, decimal Hours, string? Comment, DateTime CreatedAtUtc, DateOnly BalanceDate,
    decimal BalanceHours, decimal BalanceAfterHours)
{
    public static QueueItemResponse From(QueueItem i) => new(
        i.Id, i.Kind.ToString(), i.EmployeeId, i.EmployeeName, i.Account.ToString(), i.StartDate, i.EndDate,
        i.Hours, i.Comment, i.CreatedAtUtc, i.BalanceDate, i.BalanceHours, i.BalanceAfterHours);
}

// The reason is required when declining and optional when approving.
public record DecisionDto(string? Reason);

public class DecisionDtoValidator : AbstractValidator<DecisionDto>
{
    public DecisionDtoValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public static class RequestReviewEndpoints
{
    public static void MapRequestReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/requests").WithTags("Requests").RequireAuthorization(Policies.Accountant);

        group.MapGet("/queue", GetQueue)
            .WithName("GetRequestQueue")
            .WithSummary("Holiday, cancellation and transfer requests waiting for a decision, with balances");

        group.MapPost("/holiday/{id:guid}/approve", ApproveHoliday)
            .WithValidation<DecisionDto>()
            .ProducesValidationProblem()
            .WithName("ApproveHolidayRequest")
            .WithSummary("Approve holiday or a cancellation (writes ledger entries)");

        group.MapPost("/holiday/{id:guid}/decline", DeclineHoliday)
            .WithValidation<DecisionDto>()
            .ProducesValidationProblem()
            .WithName("DeclineHolidayRequest")
            .WithSummary("Decline holiday or a cancellation, with a reason");

        group.MapPost("/transfer/{id:guid}/approve", ApproveTransfer)
            .WithValidation<DecisionDto>()
            .ProducesValidationProblem()
            .WithName("ApproveTransferRequest")
            .WithSummary("Approve a transfer to the next period (writes ledger entries)");

        group.MapPost("/transfer/{id:guid}/decline", DeclineTransfer)
            .WithValidation<DecisionDto>()
            .ProducesValidationProblem()
            .WithName("DeclineTransferRequest")
            .WithSummary("Decline a transfer, with a reason");
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ApproveHoliday(Guid id,
        DecisionDto decision, ClaimsPrincipal principal, RequestReviewService service, CancellationToken ct) =>
        ChangeResults.ToHttp(await service.DecideHolidayAsync(id, true, decision.Reason, principal.GetUserId(), ct));

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeclineHoliday(Guid id,
        DecisionDto decision, ClaimsPrincipal principal, RequestReviewService service, CancellationToken ct) =>
        ChangeResults.ToHttp(await service.DecideHolidayAsync(id, false, decision.Reason, principal.GetUserId(), ct));

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ApproveTransfer(Guid id,
        DecisionDto decision, ClaimsPrincipal principal, RequestReviewService service, CancellationToken ct) =>
        ChangeResults.ToHttp(await service.DecideTransferAsync(id, true, decision.Reason, principal.GetUserId(), ct));

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeclineTransfer(Guid id,
        DecisionDto decision, ClaimsPrincipal principal, RequestReviewService service, CancellationToken ct) =>
        ChangeResults.ToHttp(await service.DecideTransferAsync(id, false, decision.Reason, principal.GetUserId(), ct));

    private static async Task<Ok<List<QueueItemResponse>>> GetQueue(RequestReviewService service,
        CancellationToken ct) =>
        TypedResults.Ok((await service.GetQueueAsync(ct)).Select(QueueItemResponse.From).ToList());
}
