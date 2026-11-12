using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }

    /// <summary>Knyttet medarbejder (null for fx ren admin-bruger).</summary>
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}