using System.Security.Claims;
using API.Auth;
using API.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Service.Abstractions;
using Service.Auth;

namespace API.Endpoints;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAtUtc);
public record MeResponse(Guid UserId, string Email, string Role, Guid? EmployeeId, string? EmployeeName);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithValidation<LoginRequest>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("Login")
            .WithSummary("Log in with company email and password. Send the token as 'Authorization: Bearer <token>'.");

        group.MapGet("/me", GetMe)
            .RequireAuthorization()
            .WithName("GetMe")
            .WithSummary("The logged-in user");
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Login(
        LoginRequest request, AuthService auth, LockoutSettings lockout, ILoggerFactory loggers, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request.Email, request.Password, ct);

        if (!result.Succeeded || result.Token is null)
        {
            // the reason is only logged, the caller gets the same answer no matter why it failed
            loggers.CreateLogger("Auth").LogWarning("Failed login for {Email}: {Reason}", request.Email, result.FailReason);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Login failed",
                detail: $"Wrong email or password. After {lockout.MaxFailedAttempts} failed attempts in a row " +
                        $"the account is locked for {lockout.LockoutMinutes} minutes.");
        }

        return TypedResults.Ok(new LoginResponse(result.Token.Token, result.Token.ExpiresAtUtc));
    }

    private static async Task<Results<Ok<MeResponse>, UnauthorizedHttpResult>> GetMe(
        ClaimsPrincipal principal, IUserRepository users, IEmployeeRepository employees, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(principal.GetUserId(), ct);
        if (user is null)
            return TypedResults.Unauthorized(); // the login was deleted after the token was made

        var employee = user.EmployeeId is { } id ? await employees.GetByIdAsync(id, ct) : null;
        return TypedResults.Ok(new MeResponse(user.Id, user.Email, user.Role.ToString(), user.EmployeeId, employee?.Name));
    }
}
