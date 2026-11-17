using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>
/// A login. Every employee has their own login (name@businessesbjerg.com).
/// The accountant and the system owner have separate logins (only one of each),
/// so they are not linked to an employee.
/// </summary>
public class User
{
    public const string CompanyEmailDomain = "@businessesbjerg.com";

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }

    /// <summary>Linked employee (null for the accountant and the system owner).</summary>
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>Failed logins in a row. Goes back to 0 after a successful login or when the account gets locked.</summary>
    public int FailedLoginCount { get; set; }

    /// <summary>The account can't log in before this time (null = not locked).</summary>
    public DateTime? LockoutEndUtc { get; set; }

    public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc > nowUtc;

    /// <summary>Emails are stored in lower case without spaces, so "Anna@..." and "anna@..." is the same login.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
