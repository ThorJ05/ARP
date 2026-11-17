using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace API.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>The employee behind the login (null for the accountant and the system owner).</summary>
    public static Guid? GetEmployeeId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(AuthClaims.EmployeeId), out var id) ? id : null;
}
