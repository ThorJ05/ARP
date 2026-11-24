using Microsoft.Playwright;

namespace BusinessEsbjerg.Holiday.Tests.E2E.Pages;

// The employee's page: send a holiday request and see your requests.
public class EmployeePage(IPage page)
{
    // the card with the heading "Request holiday" (the transfer form has fields with the same names)
    private ILocator RequestForm => page.Locator("section")
        .Filter(new() { Has = page.GetByRole(AriaRole.Heading, new() { Name = "Request holiday" }) });

    public async Task SendHolidayRequest(DateOnly start, DateOnly end, string comment)
    {
        await RequestForm.GetByLabel("First day").FillAsync(start.ToString("yyyy-MM-dd"));
        await RequestForm.GetByLabel("Last day").FillAsync(end.ToString("yyyy-MM-dd"));
        await RequestForm.GetByLabel("Comment (optional)").FillAsync(comment);
        await RequestForm.GetByRole(AriaRole.Button, new() { Name = "Send request" }).ClickAsync();
    }

    // the green message after sending
    public ILocator SentMessage => page.GetByText("was sent to the accountant");

    // the rows in the tables that contain the text
    public ILocator RequestRows(string text) => page.GetByRole(AriaRole.Row).Filter(new() { HasText = text });
}