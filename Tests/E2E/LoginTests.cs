    using BusinessEsbjerg.Holiday.Tests.E2E.Pages;
    using Microsoft.Playwright;

    namespace BusinessEsbjerg.Holiday.Tests.E2E;

    // E2E tests need the whole system running, so CI skips them (--filter "Category!=E2E").
    [Trait("Category", "E2E")]
    public class LoginTests : BrowserTest
    {
        [Theory]
        [InlineData("anna@businessesbjerg.com", "Test1234!", true)]
        [InlineData("nobody@businessesbjerg.com", "Test1234!", false)]
        public async Task Login_shows_the_right_page_or_an_error(string email, string password, bool shouldWork)
        {
            var login = new LoginPage(Page);
            await login.LogInAs(email, password);

            if (shouldWork)
                await Assertions.Expect(login.LogOutButton).ToBeVisibleAsync();
            else
                await Assertions.Expect(login.Error).ToBeVisibleAsync();
        }
    }