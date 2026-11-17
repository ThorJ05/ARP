using System.ComponentModel.DataAnnotations;

namespace API.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";

    /// <summary>Key used to sign tokens. For the deployed system it's set with fly secrets, never in git.</summary>
    [Required, MinLength(32)]
    public string Key { get; set; } = "";

    [Range(5, 1440)]
    public int LifetimeMinutes { get; set; } = 60;
}
