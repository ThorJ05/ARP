using BusinessEsbjerg.Holiday.Domain.Entities;
using BusinessEsbjerg.Holiday.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BusinessEsbjerg.Holiday.Infrastructure.Data;


public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedRuleParametersAsync(db);
        await SeedCompanyDaysOffAsync(db);
        await SeedEmployeesAsync(db);
        await SeedUsersAsync(db);
    }

    private static async Task SeedRuleParametersAsync(AppDbContext db)
    {
        if (await db.RuleParameters.AnyAsync()) return;

        var start = new DateOnly(2020, 9, 1); // ny ferielov i kraft
        RuleParameter P(string key, string value, string desc, DateOnly? from = null) =>
            new() { Key = key, Value = value, Description = desc, EffectiveFrom = from ?? start };

        db.RuleParameters.AddRange(
            P(RuleKeys.Weeks1To5DaysPerMonth, "2.08", "Uge 1-5: feriedage optjent pr. måned"),
            P(RuleKeys.Week6Enabled, "true", "6. ferieuge aktiv"),
            P(RuleKeys.Week6HoursPerMonth, "3.083", "6. ferieuge: timer pr. måned (fuldtid)"),
            P(RuleKeys.Week6HoursPerCalendarDay, "0.103", "6. ferieuge: timer pr. kalenderdag ved delmåned"),
            P(RuleKeys.FullTimeWeeklyHours, "37", "Fuldtid: timer pr. uge"),
            P(RuleKeys.MainHolidayDays, "15", "Hovedferie: sammenhængende arbejdsdage"),
            P(RuleKeys.MainHolidayWindowStart, "05-01", "Hovedferie: vindue start (MM-dd)"),
            P(RuleKeys.MainHolidayWindowEnd, "09-30", "Hovedferie: vindue slut (MM-dd)"),
            P(RuleKeys.NoticeMainHolidayMonths, "3", "Varsel for hovedferie (måneder)"),
            P(RuleKeys.NoticeOtherHolidayMonths, "1", "Varsel for øvrig ferie (måneder)"),
            P(RuleKeys.SupplementBasePercent, "1.00", "Ferietillæg: Ferieaftalen (%)"),
            // Hævet ferietillæg: 0,95 % indtil 1. maj 2024, derefter 1,48 %
            P(RuleKeys.SupplementRaisedPercent, "0.95", "Ferietillæg: forhøjet (%)"),
            P(RuleKeys.SupplementRaisedPercent, "1.48", "Ferietillæg: forhøjet (%)", new DateOnly(2024, 5, 1)),
            P(RuleKeys.PayoutPercentWeeks1To5, "12.5", "Feriegodtgørelse/udbetaling uge 1-5 (%)"),
            P(RuleKeys.PayoutPercentWeek6, "2.5", "Feriegodtgørelse/udbetaling uge 6 (%)"),
            P(RuleKeys.TransferDeadlineWeek5, "12-31", "Overførselsfrist 5. uge (MM-dd)"),
            P(RuleKeys.TransferDeadlineWeek6, "09-30", "Overførselsfrist uge 6 (MM-dd)")
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedCompanyDaysOffAsync(AppDbContext db)
    {
        if (await db.CompanyDaysOff.AnyAsync()) return;

        foreach (var year in new[] { 2026, 2027, 2028 })
        {
            db.CompanyDaysOff.Add(new CompanyDayOff { Date = new DateOnly(year, 12, 24), Name = "Juleaftensdag" });
            db.CompanyDaysOff.Add(new CompanyDayOff { Date = new DateOnly(year, 12, 31), Name = "Nytårsaftensdag" });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedEmployeesAsync(AppDbContext db)
    {
        if (await db.Employees.AnyAsync()) return;

        // Fuldtid
        var anna = NewEmployee("Anna Fuldtid", new DateOnly(2023, 1, 1), PayType.Salaried);
        // Deltid: man-ons 7,5 t = 22,5 t/uge
        var bo = NewEmployee("Bo Deltid", new DateOnly(2024, 3, 1), PayType.Salaried);
        // Timelønnet
        var carla = NewEmployee("Carla Timeløn", new DateOnly(2025, 6, 1), PayType.Hourly);
        // Ny i måneden
        var dan = NewEmployee("Dan Nystartet", new DateOnly(2026, 11, 3), PayType.Salaried);
        // Fratrådt
        var erik = NewEmployee("Erik Fratrådt", new DateOnly(2021, 2, 1), PayType.Salaried);
        erik.TerminationDate = new DateOnly(2026, 9, 30);

        db.Employees.AddRange(anna, bo, carla, dan, erik);
        await db.SaveChangesAsync(); // giver Id'er

        foreach (var e in new[] { anna, carla, dan, erik })
            db.WorkScheduleEntries.Add(WorkScheduleEntry.FullTime(e.Id, e.HireDate));

        db.WorkScheduleEntries.Add(new WorkScheduleEntry
        {
            EmployeeId = bo.Id,
            ValidFrom = bo.HireDate,
            MonHours = 7.5m, TueHours = 7.5m, WedHours = 7.5m
        });

        foreach (var e in new[] { anna, bo, carla, dan, erik })
        {
            db.HolidayAccounts.Add(new HolidayAccount { EmployeeId = e.Id, Type = AccountType.Weeks1To5 });
            db.HolidayAccounts.Add(new HolidayAccount { EmployeeId = e.Id, Type = AccountType.Week6 });
        }

        // Lønhistorik for Anna (ferieberettiget løn pr. måned), så DKK-beregning kan testes
        for (var month = new DateOnly(2025, 9, 1); month <= new DateOnly(2026, 10, 1); month = month.AddMonths(1))
        {
            db.MonthlySalaryRecords.Add(new MonthlySalaryRecord
            {
                EmployeeId = anna.Id,
                Month = month,
                QualifyingSalary = 40000m,
                HoursWorked = 160.33m
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var hasher = new PasswordHasher<User>();
        var anna = await db.Employees.FirstAsync(e => e.Name.StartsWith("Anna"));

        User NewUser(string email, Role role, int? employeeId)
        {
            var u = new User { Email = email, Role = role, EmployeeId = employeeId };
            u.PasswordHash = hasher.HashPassword(u, "Test1234!");
            return u;
        }

        db.Users.AddRange(
            NewUser("anna@businessesbjerg.dk", Role.Employee, anna.Id),
            NewUser("regnskab@businessesbjerg.dk", Role.Accounting, null),
            NewUser("admin@businessesbjerg.dk", Role.Admin, null)
        );
        await db.SaveChangesAsync();
    }

    private static Employee NewEmployee(string name, DateOnly hired, PayType payType) =>
        new() { Name = name, HireDate = hired, PayType = payType };
}