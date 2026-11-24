using BusinessEsbjerg.Holiday.Tests.E2E.Pages;
using Microsoft.Playwright;

namespace BusinessEsbjerg.Holiday.Tests.E2E;

// The whole flow through the website: employee → accountant → employee.
// The accountant declines, so the test can run again and again without using Anna's balance.
[Trait("Category", "E2E")]
public class HolidayRequestFlowTests : BrowserTest
{
    private const string Password = "Test1234!";
    private const string Anna = "anna@businessesbjerg.com";
    private const string Accountant = "regnskab@businessesbjerg.com";

    // the API uses today's date, so the holiday is a Monday 12 weeks from now (notice rules are met)
    private static DateOnly MondayInWeeks(int weeks)
    {
        var date = DateOnly.FromDateTime(DateTime.Today).AddDays(7 * weeks);
        while (date.DayOfWeek != DayOfWeek.Monday)
            date = date.AddDays(1);
        return date;
    }

    [Fact]
    public async Task Employee_sees_the_accountants_decision()
    {
        var start = MondayInWeeks(12);
        var end = start.AddDays(4); // Friday

        // unique per run, so the test finds its own request even after earlier runs
        var stamp = $"e2e {DateTime.Now.Ticks}";
        var reason = $"Declined by {stamp}";

        var login = new LoginPage(Page);
        var employee = new EmployeePage(Page);
        var accountant = new AccountantPage(Page);

        // 1. Anna sends a holiday request
        await login.LogInAs(Anna, Password);
        await employee.SendHolidayRequest(start, end, stamp);
        await Assertions.Expect(employee.SentMessage).ToBeVisibleAsync();
        await Assertions.Expect(employee.RequestRows(start.ToString("yyyy-MM-dd"))
            .Filter(new() { HasText = "Waiting for approval" })).ToHaveCountAsync(1);
        await login.LogOutButton.ClickAsync();

        // 2. The accountant declines it, with a reason
        await login.LogInAs(Accountant, Password);
        await accountant.DeclineRequest(stamp, reason);
        await Assertions.Expect(accountant.DeclinedMessage).ToBeVisibleAsync();
        await login.LogOutButton.ClickAsync();

        // 3. Anna sees the decision
        await login.LogInAs(Anna, Password);
        await Assertions.Expect(employee.RequestRows(reason)).ToContainTextAsync("Rejected");
    }
}