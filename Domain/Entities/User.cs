using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

// A login. Every employee has their own login (name@businessesbjerg.com).
// The accountant and the system owner have separate logins (only one of each),
// so they are not linked to an employee.
public class User
{
    public const string CompanyEmailDomain = "@businessesbjerg.com";

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }

    // Linked employee (null for the accountant and the system owner).
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    // Failed logins in a row. Goes back to 0 after a successful login or when the account gets locked.
    public int FailedLoginCount { get; set; }

    // The account can't log in before this time (null = not locked).
    public DateTime? LockoutEndUtc { get; set; }

    public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc > nowUtc;

    // Emails are stored in lower case without spaces, so "Anna@..." and "anna@..." is the same login.
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
