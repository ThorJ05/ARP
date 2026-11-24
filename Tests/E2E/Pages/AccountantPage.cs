using Microsoft.Playwright;

namespace BusinessEsbjerg.Holiday.Tests.E2E.Pages;

// The accountant's page: decline a request in the queue.
public class AccountantPage(IPage page)
{
    // finds the request by its (unique) comment, writes the reason and clicks Decline
    public async Task DeclineRequest(string comment, string reason)
    {
        var row = page.GetByRole(AriaRole.Row).Filter(new() { HasText = comment });
        await row.GetByPlaceholder("Needed to decline").FillAsync(reason);
        await row.GetByRole(AriaRole.Button, new() { Name = "Decline" }).ClickAsync();
    }

    // the green message after a decision, e.g. "Anna Fulltime: declined."
    // ": declined." so it doesn't also match a reason that starts with "Declined"
    public ILocator DeclinedMessage => page.GetByText(": declined.");
}