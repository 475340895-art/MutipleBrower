using Microsoft.Playwright;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Services;

public class PlaywrightService
{
    private static PlaywrightService? _instance;
    public static PlaywrightService Instance => _instance ??= new PlaywrightService();
    
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _semaphore = new(5);
    
    public async Task InitializeAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
    }
    
    public async Task<IBrowser> LaunchBrowserAsync(BrowserEnvironment env)
    {
        if (_playwright == null) await InitializeAsync();
        if (_browser == null)
        {
            _browser = await _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[] { "--disable-blink-features=AutomationControlled" }
            });
        }
        return _browser;
    }
    
    public async Task<IBrowserContext> CreateContextAsync(BrowserEnvironment env)
    {
        var browser = await LaunchBrowserAsync(env);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = env.UserAgent,
            ViewportSize = ParseResolution(env.Resolution),
            Locale = env.Languages?.Split(',').FirstOrDefault() ?? "en-US",
            TimezoneId = env.Timezone ?? "UTC"
        });
        return context;
    }
    
    public async Task CloseContextAsync(IBrowserContext context)
    {
        await context.CloseAsync();
    }
    
    public async Task<IPage> OpenUrlAsync(IBrowserContext context, string url)
    {
        var page = await context.NewPageAsync();
        await page.GotoAsync(url);
        return page;
    }
    
    private static ViewportSize? ParseResolution(string? resolution)
    {
        if (string.IsNullOrEmpty(resolution)) return null;
        var parts = resolution.Split('x');
        if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
            return new ViewportSize { Width = w, Height = h };
        return null;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }
        _playwright?.Dispose();
    }
}
