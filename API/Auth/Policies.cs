namespace API.Auth;

/// <summary>Names of the authorization policies. See the access matrix in README.md.</summary>
public static class Policies
{
    public const string Employee = "Employee";
    public const string Accountant = "Accountant";
    public const string SystemOwner = "SystemOwner";
}

/// <summary>Our own claim names in the token.</summary>
public static class AuthClaims
{
    public const string Role = "role";
    public const string EmployeeId = "employee_id";
}
