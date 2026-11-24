namespace BusinessEsbjerg.Holiday.Tests;

// The API uses today's date, so tests that go through the API can't use fixed dates.
public static class TestDates
{
    public static DateOnly MondayInWeeks(int weeks)
    {
        var date = DateOnly.FromDateTime(DateTime.Today).AddDays(7 * weeks);
        while (date.DayOfWeek != DayOfWeek.Monday)
            date = date.AddDays(1);
        return date;
    }
}
