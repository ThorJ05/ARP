using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Domain.Balances;

// When earned holiday can be used and when it expires.
// Weeks 1-5: earned 1 Sep - 31 Aug, can be used right away and until 31 Dec the year after (16 months).
// Week 6: earned 1 Jan - 31 Dec, can be used from 1 May the year after until 30 April the year after that.
public static class HolidayPeriods
{
    // The last day holiday earned on the given date can be used.
    public static DateOnly ExpiresOn(AccountType type, DateOnly earned) => type switch
    {
        AccountType.Weeks1To5 => new DateOnly(EarningYearStart(earned) + 1, 12, 31),
        AccountType.Week6 => new DateOnly(earned.Year + 2, 4, 30),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // The first day holiday with this expiry date can be used.
    public static DateOnly AvailableFrom(AccountType type, DateOnly expiresOn) => type switch
    {
        // concurrent holiday: what is earned can be used right away
        AccountType.Weeks1To5 => new DateOnly(expiresOn.Year - 1, 9, 1),
        AccountType.Week6 => new DateOnly(expiresOn.Year - 1, 5, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // the earning year for weeks 1-5 starts on 1 September
    private static int EarningYearStart(DateOnly date) => date.Month >= 9 ? date.Year : date.Year - 1;
}
