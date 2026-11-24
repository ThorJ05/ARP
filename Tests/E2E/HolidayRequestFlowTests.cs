using BusinessEsbjerg.Holiday.Tests.E2E.Pages;
using BusinessEsbjerg.Holiday.Tests.Integration;
using Microsoft.Playwright;
using static BusinessEsbjerg.Holiday.Tests.TestDates;

namespace BusinessEsbjerg.Holiday.Tests.E2E;

// The whole flow through the website: employee → accountant → employee.
// The accountant declines, so the test can run again and again without using Anna's balance.
[Trait("Category", "E2E")]
public class HolidayRequestFlowTests : BrowserTest
{
    [E2EFact]
    public async Task Employee_sees_decision()
    {
        // 12 weeks ahead, so the notice rules are met
        var start = MondayInWeeks(12);
        var end = start.AddDays(4); // Friday

        // unique per run, so the test finds its own request even after earlier runs
        var stamp = $"e2e {DateTime.Now.Ticks}";
        var reason = $"Declined by {stamp}";

        var login = new LoginPage(Page);
        var employee = new EmployeePage(Page);
        var accountant = new AccountantPage(Page);

        // 1. Anna sends a holiday request
        await login.LogInAs(TestUsers.Anna, TestUsers.Password);
        await employee.SendHolidayRequest(start, end, stamp);
        await Assertions.Expect(employee.SentMessage).ToBeVisibleAsync();
        await Assertions.Expect(employee.RequestRows(start.ToString("yyyy-MM-dd"))
            .Filter(new() { HasText = "Waiting for approval" })).ToHaveCountAsync(1);
        await login.LogOutButton.ClickAsync();

        // 2. The accountant declines it, with a reason
        await login.LogInAs(TestUsers.Accountant, TestUsers.Password);
        await accountant.DeclineRequest(stamp, reason);
        await Assertions.Expect(accountant.DeclinedMessage).ToBeVisibleAsync();
        await login.LogOutButton.ClickAsync();

        // 3. Anna sees the decision
        await login.LogInAs(TestUsers.Anna, TestUsers.Password);
        await Assertions.Expect(employee.RequestRows(reason)).ToContainTextAsync("Rejected");
    }
}