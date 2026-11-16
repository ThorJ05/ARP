using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }

    /// <summary>Linked employee (null for a pure Admin user).</summary>
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}