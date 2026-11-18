namespace BusinessEsbjerg.Holiday.Service;

// The result of a change. NotFound = the employee doesn't exist, Error = the change isn't allowed.
public record ChangeResult(bool NotFound, string? Error)
{
    public static readonly ChangeResult Ok = new(false, null);
    public static readonly ChangeResult Missing = new(true, null);
    public static ChangeResult Invalid(string error) => new(false, error);
}
