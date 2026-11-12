namespace Service.Abstractions;

public interface IClock
{
    DateOnly Today { get; }
}