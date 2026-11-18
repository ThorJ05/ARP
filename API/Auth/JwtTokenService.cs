using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BusinessEsbjerg.Holiday.Api.Auth;

// Creates the JWT the client gets after logging in. It holds the user id, email, role and employee id.
public class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : ITokenService
{
    public AuthToken CreateToken(User user)
    {
        var jwt = options.Value;
        var now = clock.UtcNow;
        var expires = now.AddMinutes(jwt.LifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Claims = Claims(user),
            SigningCredentials = new SigningCredentials(AuthSetup.SigningKey(jwt.Key), SecurityAlgorithms.HmacSha256)
        };

        return new AuthToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    private static Dictionary<string, object> Claims(User user)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email,
            [AuthClaims.Role] = user.Role.ToString()
        };
        if (user.EmployeeId is { } employeeId)
            claims[AuthClaims.EmployeeId] = employeeId.ToString();

        return claims;
    }
}
