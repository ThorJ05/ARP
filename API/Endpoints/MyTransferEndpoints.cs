using System.Security.Claims;
using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Api.Validation;
using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Domain.Requests;
using BusinessEsbjerg.Holiday.Service.Requests;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

// What the website sends. Account is "Weeks1To5" or "Week6".
public record TransferRequestDto(string Account, DateOnly FromPeriodStart, decimal Hours, string? Comment)
{
    public NewTransferRequest ToNewRequest() =>
        new(Enum.Parse<AccountType>(Account), FromPeriodStart, Hours);
}

// What the API sends back for each of the employee's transfer requests.
public record MyTransferResponse(
    Guid Id, string Account, DateOnly FromPeriodStart, decimal Hours, string Status,
    string? Comment, string? DecisionReason, DateTime CreatedAtUtc)
{
    public static MyTransferResponse From(TransferRequest t) => new(
        t.Id, t.AccountType.ToString(), t.FromPeriodStart, t.Hours, t.Status.ToString(),
        t.Comment, t.DecisionReason, t.CreatedAtUtc);
}

public record CreatedTransferResponse(Guid Id);

// Server-side validation of the input's format (the holiday rules are checked in TransferRules).
public class TransferRequestDtoValidator : AbstractValidator<TransferRequestDto>
{
    public TransferRequestDtoValidator()
    {
        RuleFor(x => x.Account)
            .Must(a => a is nameof(AccountType.Weeks1To5) or nameof(AccountType.Week6))
            .WithMessage("Account must be Weeks1To5 or Week6.");
        RuleFor(x => x.FromPeriodStart).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public static class MyTransferEndpoints
{
    public static void MapMyTransferEndpoints(this IEndpointRouteBuilder app)
    {
        // the employee id comes from the token, so employees only see and create their own requests
        var group = app.MapGroup("/api/me/transfers{id}").WithTags("Me").RequireAuthorization(Policies.Employee);

        group.MapGet("/", GetMyTransfers)
            .WithName("GetMyTransfers")
            .WithSummary("My transfer requests and their status");

        group.MapPost("/", Create)
            .WithValidation<TransferRequestDto>()
            .ProducesValidationProblem()
            .WithName("CreateTransfer")
            .WithSummary("Ask to transfer unused holiday to the next period");
    }

    private static async Task<Ok<List<MyTransferResponse>>> GetMyTransfers(
        ClaimsPrincipal principal, TransferRequestService transfers, CancellationToken ct)
    {
        var mine = await transfers.GetForEmployeeAsync(principal.GetEmployeeId()!.Value, ct);
        return TypedResults.Ok(mine.Select(MyTransferResponse.From).ToList());
    }

    private static async Task<Results<Created<CreatedTransferResponse>, ValidationProblem, NotFound>> Create(
        TransferRequestDto request, ClaimsPrincipal principal, TransferRequestService transfers, CancellationToken ct)
    {
        var created = await transfers.CreateAsync(
            principal.GetEmployeeId()!.Value, request.ToNewRequest(), request.Comment, ct);

        if (created is null) return TypedResults.NotFound();
        if (created.TransferId is not { } id)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["transfer"] = created.Errors.ToArray()
            });

        return TypedResults.Created($"/api/me/transfers/{id}", new CreatedTransferResponse(id));
    }
}