namespace BusinessEsbjerg.Holiday.Domain.Calendar;

// Danish public holidays. Most of them move with Easter. Store Bededag is not a holiday since 2024.
public static class DanishPublicHolidays
{
    public static IReadOnlyList<DateOnly> In(int year)
    {
        var easter = EasterSunday(year);
        return
        [
            new DateOnly(year, 1, 1),   // New Year's Day
            easter.AddDays(-3),         // Maundy Thursday
            easter.AddDays(-2),         // Good Friday
            easter,                     // Easter Sunday
            easter.AddDays(1),          // Easter Monday
            easter.AddDays(39),         // Ascension Day
            easter.AddDays(49),         // Whit Sunday
            easter.AddDays(50),         // Whit Monday
            new DateOnly(year, 12, 25), // Christmas Day
            new DateOnly(year, 12, 26)  // Boxing Day
        ];
    }

    // the "anonymous Gregorian" algorithm
    public static DateOnly EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }
}
