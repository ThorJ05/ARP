namespace BusinessEsbjerg.Holiday.Domain.Entities;


public class MonthlySalaryRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>First day of the month.</summary>
    public DateOnly Month { get; set; }

    /// <summary> holidaymoney.</summary>
    public decimal QualifyingSalary { get; set; }
    public decimal HoursWorked { get; set; }
}