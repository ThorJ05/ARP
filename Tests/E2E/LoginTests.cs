using BusinessEsbjerg.Holiday.Tests.E2E.Pages;
using BusinessEsbjerg.Holiday.Tests.Integration;
using Microsoft.Playwright;

namespace BusinessEsbjerg.Holiday.Tests.E2E;

[Trait("Category", "E2E")]
public class LoginTests : BrowserTest
{
    [E2EFact]
    public async Task Valid_login_shows_log_out()
    {
        var login = new LoginPage(Page);

        await login.LogInAs(TestUsers.Anna, TestUsers.Password);

        await Assertions.Expect(login.LogOutButton).ToBeVisibleAsync();
    }

    [E2EFact]
    public async Task Unknown_user_sees_error()
    {
        var login = new LoginPage(Page);

        await login.LogInAs("nobody@businessesbjerg.com", TestUsers.Password);

        await Assertions.Expect(login.Error).ToBeVisibleAsync();
    }
}
