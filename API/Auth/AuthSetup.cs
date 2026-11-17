using System.Text;
using BusinessEsbjerg.Holiday.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Service.Abstractions;

namespace API.Auth;

public static class AuthSetup
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart(); // the API won't start without a signing key

        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // how incoming tokens are checked, using the same settings the tokens were made with
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false; // keep claim names like "sub" and "role" as they are in the token
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Value.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = SigningKey(jwt.Value.Key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = JwtRegisteredClaimNames.Email,
                    RoleClaimType = AuthClaims.Role
                };
            });

        services.AddAuthorizationBuilder()
            // secure by default: every endpoint needs a logged-in user unless it says AllowAnonymous
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Employee, p => p.RequireRole(nameof(Role.Employee)).RequireClaim(AuthClaims.EmployeeId))
            .AddPolicy(Policies.Accountant, p => p.RequireRole(nameof(Role.Accountant)))
            .AddPolicy(Policies.SystemOwner, p => p.RequireRole(nameof(Role.SystemOwner)))
            .AddPolicy(Policies.AccountantOrSystemOwner,
                p => p.RequireRole(nameof(Role.Accountant), nameof(Role.SystemOwner)));

        return services;
    }

    public static SymmetricSecurityKey SigningKey(string key) => new(Encoding.UTF8.GetBytes(key));
}
