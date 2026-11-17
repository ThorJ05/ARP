namespace BusinessEsbjerg.Holiday.Service.Abstractions;

public interface IClock
{
    DateOnly Today { get; }
    DateTime UtcNow { get; }
}
