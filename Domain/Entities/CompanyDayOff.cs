namespace BusinessEsbjerg.Holiday.Domain.Entities;

/// <summary>comapny free day.</summary>
public class CompanyDayOff
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateOnly Date { get; set; }
    public string Name { get; set; } = "";
}