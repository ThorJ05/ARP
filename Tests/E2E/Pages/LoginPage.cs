using Microsoft.Playwright;

namespace BusinessEsbjerg.Holiday.Tests.E2E.Pages;

// Hides the clicks on the login page, so the tests read like a story.
public class LoginPage(IPage page)
{
    public async Task LogInAs(string email, string password)
    {
        await page.GotoAsync("/");
        await page.GetByLabel("Email").FillAsync(email);
        await page.GetByLabel("Password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
    }

    // the red error message (role="alert" in LoginPage.tsx)
    public ILocator Error => page.GetByRole(AriaRole.Alert);

    // only shown when you are logged in (Layout.tsx)
    public ILocator LogOutButton => page.GetByRole(AriaRole.Button, new() { Name = "Log out" });
}