using BusinessEsbjerg.Holiday.Domain.Balances;
using BusinessEsbjerg.Holiday.Domain.Entities;
using static BusinessEsbjerg.Holiday.Tests.Unit.TestData;

namespace BusinessEsbjerg.Holiday.Tests.Unit;

public class AccrualCalculatorTests
{
    [Fact]
    public void Full_time_month()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));

        var accrual = AccrualCalculator.ForMonth(employee, new DateOnly(2026, 3, 1), Rules())!;

        Assert.Equal(15.39m, accrual.Weeks1To5Hours); // 2.08 days * 7.4 h
         
        Assert.Equal(3.08m, accrual.Week6Hours);
        Assert.Equal(new DateOnly(2026, 3, 31), accrual.EarnedOn);
    }

    [Fact]
    public void Part_time_month()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));
        employee.WorkSchedule = [PartTime(employee.Id, employee.HireDate)];

        var accrual = AccrualCalculator.ForMonth(employee, new DateOnly(2026, 3, 1), Rules())!;

        
        Assert.Equal(15.60m, accrual.Weeks1To5Hours); // 2.08 days * 7.5 h
        Assert.Equal(1.87m, accrual.Week6Hours);      // 3.083 * 22.5 / 37
    }

    [Fact]
    public void Starts_mid_month()
    { 
        var employee = FullTimeEmployee(new DateOnly(2026, 4, 16)); // 15 of 30 days

        var accrual = AccrualCalculator.ForMonth(employee, new DateOnly(2026, 4, 1), Rules())!;
        

        Assert.Equal(7.70m, accrual.Weeks1To5Hours); // 2.08 * 15/30 * 7.4
        Assert.Equal(1.55m, accrual.Week6Hours);     // 0.103 * 15 calendar days
    }

    [Fact]
    public void Ends_mid_month()
    {
        var employee = FullTimeEmployee(new DateOnly(2025, 1, 1), terminated: new DateOnly(2026, 6, 10));
        

        var accrual = AccrualCalculator.ForMonth(employee,  new DateOnly(2026, 6, 1), Rules())!;

        
        Assert.Equal(new DateOnly(2026, 6, 10), accrual.EarnedOn);
        Assert.Equal(5.13m, accrual.Weeks1To5Hours); // 2.08 * 10/30 * 7.4
        Assert.Equal(1.03m, accrual.Week6Hours);     // 0.103 * 10
    }

    [Fact]
    public void Nothing_outside_employment()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 5, 1), terminated: new DateOnly(2026, 8, 31));

        Assert.Null(AccrualCalculator.ForMonth(employee, new DateOnly(2026, 4, 1), Rules()));
        Assert.Null(AccrualCalculator.ForMonth(employee, new DateOnly(2026, 9, 1), Rules()));
    }

    [Fact]
    public void Rule_change_from_start_date()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));
        
        var rules = Rules(Rule(RuleKeys.Weeks1To5DaysPerMonth, "2.16", new DateOnly(2027, 1, 1)));

        var before = AccrualCalculator.ForMonth(employee, new DateOnly(2026, 12, 1), rules)!;
        var after = AccrualCalculator.ForMonth(employee, new DateOnly(2027, 1, 1), rules)!;

        Assert.Equal(15.39m, before.Weeks1To5Hours); // still 2.08
        Assert.Equal(15.98m, after.Weeks1To5Hours);  // 2.16 * 7.4
    }

    [Fact]
    public void Week6_can_be_turned_off()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));
        var rules = Rules(Rule(RuleKeys.Week6Enabled, "false", new DateOnly(2026, 1, 1)));

        var accrual = AccrualCalculator.ForMonth(employee, new DateOnly(2026, 2, 1), rules)!;

        Assert.Equal(0m, accrual.Week6Hours);
        Assert.Equal(15.39m, accrual.Weeks1To5Hours);
    }

    [Fact]
    public void Schedule_change_mid_month()
    {
        var employee = FullTimeEmployee(new DateOnly(2026, 1, 1));
        employee.WorkSchedule.Add(PartTime(employee.Id, new DateOnly(2026, 6, 16)));

        var accrual = AccrualCalculator.ForMonth(employee, new DateOnly(2026, 6, 1), Rules())!;
        
        Assert.Equal(15.50m, accrual.Weeks1To5Hours); // 15 days at 7.4 h + 15 days at 7.5 
        
        
        Assert.Equal(2.48m, accrual.Week6Hours);      // 15 days full time + 15 days at 22.5/37
    }
}
