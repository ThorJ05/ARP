namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>comapny free day.</summary>
public class CompanyDayOff
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = "";
}