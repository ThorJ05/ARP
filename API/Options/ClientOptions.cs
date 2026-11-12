using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class ClientOptions
{
    public const string SectionName = "Client";

    [Required]
    public string Origin { get; set; } = "";
}
