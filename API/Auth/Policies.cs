namespace BusinessEsbjerg.Holiday.Api.Auth;

// Names of the authorization policies. See the access matrix in README.md.
public static class Policies
{
    public const string Employee = "Employee";
    public const string Accountant = "Accountant";
    public const string SystemOwner = "SystemOwner";
    public const string AccountantOrSystemOwner = "AccountantOrSystemOwner";
}

// Our own claim names in the token.
public static class AuthClaims
{
    public const string Role = "role";
    public const string EmployeeId = "employee_id";
}
