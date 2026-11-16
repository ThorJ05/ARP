using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }

    /// <summary>Linked employee (null for e.g. a pure admin user).</summary>
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}