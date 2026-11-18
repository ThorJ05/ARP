using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BusinessEsbjerg.Holiday.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    // The employee behind the login (null for the accountant and the system owner).
    public static Guid? GetEmployeeId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(AuthClaims.EmployeeId), out var id) ? id : null;
}
