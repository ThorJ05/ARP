using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class HolidayPeriodsTests
{
    [Theory]
    [InlineData("2025-09-01", "2026-12-31")] // first month of the earning year 2025/26
    [InlineData("2026-08-31", "2026-12-31")] // last day of the same earning year
    [InlineData("2026-09-30", "2027-12-31")] // next earning year
    public void Weeks1To5_expire_on_31_December_after_the_earning_year(string earned, string expires)
    {
        Assert.Equal(DateOnly.Parse(expires), HolidayPeriods.ExpiresOn(AccountType.Weeks1To5, DateOnly.Parse(earned)));
    }

    [Theory]
    [InlineData("2026-01-31", "2028-04-30")]
    [InlineData("2026-12-31", "2028-04-30")]
    public void Week6_earned_in_a_year_expires_on_30_April_two_years_later(string earned, string expires)
    {
        Assert.Equal(DateOnly.Parse(expires), HolidayPeriods.ExpiresOn(AccountType.Week6, DateOnly.Parse(earned)));
    }

    [Fact]
    public void Week6_earned_in_2026_can_be_used_from_1_May_2027()
    {
        Assert.Equal(new DateOnly(2027, 5, 1), HolidayPeriods.AvailableFrom(AccountType.Week6, new DateOnly(2028, 4, 30)));
    }
}
