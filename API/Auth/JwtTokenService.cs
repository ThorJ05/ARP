using BusinessEsbjerg.Holiday.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using BusinessEsbjerg.Holiday.Service.Abstractions;

namespace BusinessEsbjerg.Holiday.Api.Auth;

/// <summary>Creates the JWT the client gets after logging in. It holds the user id, email, role and employee id.</summary>
public class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : ITokenService
{
    public AuthToken CreateToken(User user)
    {
        var jwt = options.Value;
        var now = clock.UtcNow;
        var expires = now.AddMinutes(jwt.LifetimeMinutes);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email,
            [AuthClaims.Role] = user.Role.ToString()
        };
        if (user.EmployeeId is { } employeeId)
            claims[AuthClaims.EmployeeId] = employeeId.ToString();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Claims = claims,
            SigningCredentials = new SigningCredentials(AuthSetup.SigningKey(jwt.Key), SecurityAlgorithms.HmacSha256)
        };

        return new AuthToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
