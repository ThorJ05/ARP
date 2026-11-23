using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Enums;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class HolidayPeriodsTests
{
    // earned, expires
    public static TheoryData<DateOnly, DateOnly> Weeks1To5Expiry => new()
    {
        { new DateOnly(2025, 9, 1), new DateOnly(2026, 12, 31) },  // first day of the earning year 2025/26
        { new DateOnly(2026, 8, 31), new DateOnly(2026, 12, 31) }, // last day of the same earning year
        { new DateOnly(2026, 9, 1), new DateOnly(2027, 12, 31) }   // first day of the next earning year
    };

    [Theory]
    [MemberData(nameof(Weeks1To5Expiry))]
    public void Weeks1To5_expiry_date(DateOnly earned, DateOnly expected)
    {
        // Act
        var expires = HolidayPeriods.ExpiresOn(AccountType.Weeks1To5, earned);

        // Assert
        Assert.Equal(expected, expires);
    }

    [Theory]
    [InlineData(1)]  // January
    [InlineData(12)] // December
    public void Week6_expiry_date(int month)
    {
        // Arrange
        var earned = new DateOnly(2026, month, 1);

        // Act
        var expires = HolidayPeriods.ExpiresOn(AccountType.Week6, earned);

        // Assert
        Assert.Equal(new DateOnly(2028, 4, 30), expires);
    }

    [Fact]
    public void Weeks1To5_available_from()
    {
        // Arrange
        var expires = new DateOnly(2026, 12, 31);

        // Act
        var from = HolidayPeriods.AvailableFrom(AccountType.Weeks1To5, expires);

        // Assert
        Assert.Equal(new DateOnly(2025, 9, 1), from);
    }

    [Fact]
    public void Week6_available_from_1_May()
    {
        // Arrange
        var expires = new DateOnly(2028, 4, 30);

        // Act
        var from = HolidayPeriods.AvailableFrom(AccountType.Week6, expires);

        // Assert
        Assert.Equal(new DateOnly(2027, 5, 1), from);
    }

    // type, a date in the period, the first day of the period
    public static TheoryData<AccountType, DateOnly, DateOnly> PeriodStarts => new()
    {
        { AccountType.Weeks1To5, new DateOnly(2026, 3, 15), new DateOnly(2025, 9, 1) }, // March is in the period from September
        { AccountType.Weeks1To5, new DateOnly(2026, 9, 15), new DateOnly(2026, 9, 1) }, // September starts a new period
        { AccountType.Week6, new DateOnly(2026, 7, 15), new DateOnly(2026, 1, 1) }       // week 6 periods start on 1 January
    };

    [Theory]
    [MemberData(nameof(PeriodStarts))]
    public void Period_start_date(AccountType type, DateOnly date, DateOnly expected)
    {
        // Act
        var start = HolidayPeriods.PeriodStart(type, date);

        // Assert
        Assert.Equal(expected, start);
    }
}
