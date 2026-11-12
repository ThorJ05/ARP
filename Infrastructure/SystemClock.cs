using Service.Abstractions;

namespace Infrastructure;

public class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}