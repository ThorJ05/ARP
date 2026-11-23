using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Requests;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class HolidayCostTests
{
    private readonly Employee _employee = FullTimeEmployee(new DateOnly(2026, 1, 1));

    private static decimal Cost(Employee employee, DateOnly start, DateOnly end, HashSet<DateOnly>? daysOff = null) =>
        HolidayDays.Between(employee, start, end, daysOff ?? []).Sum(d => d.Hours);

    [Fact]
    public void Short_day_costs_less()
    {
        var friday = new DateOnly(2026, 11, 20);
        var tuesday = new DateOnly(2026, 11, 17);

        Assert.Equal(7m, Cost(_employee, friday, friday));
        Assert.Equal(7.5m, Cost(_employee, tuesday, tuesday));
    }
    

    [Fact]
    public void Week_costs_37_hours()
    {
        // Monday 16 to Sunday 22 November 2026
        Assert.Equal(37m, Cost(_employee, new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 22)));
    }

    [Fact]
    public void Days_off_cost_nothing()
    {
        var christmasEve = new DateOnly(2026, 12, 24); // Thursday
        var daysOff = new HashSet<DateOnly> { christmasEve };

        Assert.Equal(0m, Cost(_employee, christmasEve, christmasEve, daysOff));
    }
    
    

    [Fact]
    public void Cost_follows_schedule()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));
        employee.WorkSchedule.Add(PartTime(employee.Id, new DateOnly(2026, 11, 19)));

        // Wed 18 (full time 7.5) + Thu 19 and Fri 20 (part time: not working days)
        Assert.Equal(7.5m, Cost(employee, new DateOnly(2026, 11, 18), new DateOnly(2026, 11, 20)));
    }
    
    
}
