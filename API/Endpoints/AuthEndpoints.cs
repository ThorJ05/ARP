using System.Security.Claims;
using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Api.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using BusinessEsbjerg.Holiday.Service.Auth;
using BusinessEsbjerg.Holiday.Service.Users;

namespace BusinessEsbjerg.Holiday.Api.Endpoints;

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
        ClaimsPrincipal principal, UserService users, CancellationToken ct)
    {
        var me = await users.GetCurrentUserAsync(principal.GetUserId(), ct);
        if (me is null)
            return TypedResults.Unauthorized(); // the login was deleted after the token was made

        return TypedResults.Ok(new MeResponse(me.UserId, me.Email, me.Role.ToString(), me.EmployeeId, me.EmployeeName));
    }
}
