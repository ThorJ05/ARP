using BusinessEsbjerg.Holiday.Domain.Calendar;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class DanishPublicHolidaysTests
{
    [Theory]
    [InlineData(2025, "2025-04-20")]
    [InlineData(2026, "2026-04-05")]
    [InlineData(2027, "2027-03-28")]
    public void Easter_Sunday_is_calculated(int year, string easter)
    {
        Assert.Equal(DateOnly.Parse(easter), DanishPublicHolidays.EasterSunday(year));
    }

    [Fact]
    public void The_holidays_that_follow_Easter_move_with_it()
    {
        var holidays = DanishPublicHolidays.In(2026);

        Assert.Contains(new DateOnly(2026, 4, 2), holidays);  // Maundy Thursday
        Assert.Contains(new DateOnly(2026, 5, 14), holidays); // Ascension Day
        Assert.Contains(new DateOnly(2026, 5, 25), holidays); // Whit Monday
        Assert.Contains(new DateOnly(2026, 12, 26), holidays);
        Assert.Equal(10, holidays.Count);
    }
}
