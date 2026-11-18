using BusinessEsbjerg.Holiday.Domain.Entities;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class HolidayCostTests
{
    private readonly Employee _employee = FullTimeEmployee(new DateOnly(2026, 1, 1));

    [Fact]
    public void A_short_Friday_costs_less_than_a_long_Tuesday()
    {
        var friday = new DateOnly(2026, 11, 20);
        var tuesday = new DateOnly(2026, 11, 17);

        Assert.Equal(7m, _employee.GetHolidayHours(friday, friday));
        Assert.Equal(7.5m, _employee.GetHolidayHours(tuesday, tuesday));
    }
    

    [Fact]
    public void A_whole_week_costs_37_hours_and_weekends_are_free()
    {
        // Monday 16 to Sunday 22 November 2026
        Assert.Equal(37m, _employee.GetHolidayHours(new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 22)));
    }

    [Fact]
    public void Days_off_cost_nothing()
    {
        var christmasEve = new DateOnly(2026, 12, 24); // Thursday
        var daysOff = new HashSet<DateOnly> { christmasEve };

        Assert.Equal(0m, _employee.GetHolidayHours(christmasEve, christmasEve, daysOff));
    }
    
    

    [Fact]
    public void The_cost_follows_the_schedule_used_on_each_day()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));
        employee.WorkSchedule.Add(PartTime(employee.Id, new DateOnly(2026, 11, 19)));

        // Wed 18 (full time 7.5) + Thu 19 and Fri 20 (part time: not working days)
        Assert.Equal(7.5m, employee.GetHolidayHours(new DateOnly(2026, 11, 18), new DateOnly(2026, 11, 20)));
    }
    
    
}
