using Microsoft.Playwright;

namespace BusinessEsbjerg.Holiday.Tests.E2E;

// Opens a real Chromium browser before each test and closes it afterwards.
// The website (bun dev), the API and the database must be running locally.
public abstract class BrowserTest : IAsyncLifetime
{
    private const string BaseUrl = "http://localhost:3000";
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    protected IPage Page { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        // Headless = false shows the browser while the test runs (nice for a demo)
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless =  true });
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = BaseUrl });
        Page = await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}