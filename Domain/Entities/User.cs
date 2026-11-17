using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>
/// A login. Every employee has their own login (name@businessesbjerg.com).
/// The accountant and the system owner have separate logins (only one of each),
/// so they are not linked to an employee.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }

    /// <summary>Linked employee (null for the accountant and the system owner).</summary>
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}
