using Microsoft.Playwright;

namespace FingerprintBrowser.Services;

public class PlaywrightService
{
    private static PlaywrightService? _instance;
    public static PlaywrightService Instance => _instance ??= new PlaywrightService();

    private IPlaywright? _playwright;
    private IBrowserType? _chromium;
    private bool _initialized;

    private PlaywrightService() { }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _chromium = _playwright.Chromium;
        _initialized = true;
    }

    public async Task<IBrowser> LaunchAsync(BrowserTypeLaunchOptions? options = null)
    {
        await InitializeAsync();
        return await _chromium!.LaunchAsync(options ?? new BrowserTypeLaunchOptions { Headless = false });
    }

    public IPlaywright GetPlaywright() => _playwright!;
}
