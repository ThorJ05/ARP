namespace BusinessEsbjerg.Holiday.Domain.Entities;

// Company day off set by the system owner, e.g. 24 December. Doesn't cost any holiday.
public class CompanyDayOff
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateOnly Date { get; set; }
    public string Name { get; set; } = "";
}
