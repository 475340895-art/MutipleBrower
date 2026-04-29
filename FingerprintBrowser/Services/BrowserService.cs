using System.Collections.Concurrent;
using FingerprintBrowser.Models;
using Microsoft.Playwright;

namespace FingerprintBrowser.Services;

public class BrowserService
{
    private static BrowserService? _instance;
    public static BrowserService Instance => _instance ??= new BrowserService();

    private readonly ConcurrentDictionary<int, IBrowser> _runningBrowsers = new();
    private readonly ConcurrentDictionary<int, IBrowserContext> _contexts = new();
    private readonly ConcurrentDictionary<int, IPage> _pages = new();
    private readonly PlaywrightService _playwright = PlaywrightService.Instance;
    private int _maxConcurrency = 5;
    private readonly SemaphoreSlim _semaphore;

    private BrowserService()
    {
        _semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
    }

    public void SetMaxConcurrency(int count)
    {
        _maxConcurrency = count;
    }

    public async Task LaunchBrowserAsync(BrowserEnvironment env)
    {
        await _semaphore.WaitAsync();
        try
        {
            var proxy = string.IsNullOrEmpty(env.ProxyInfo) ? null : new Proxy
            {
                Server = env.ProxyInfo,
                Username = env.ProxyConfig?.Username,
                Password = env.ProxyConfig?.Password
            };

            var options = new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[] { "--disable-blink-features=AutomationControlled" }
            };

            var browser = await _playwright.LaunchAsync(options);
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = ParseResolution(env.Resolution),
                UserAgent = env.UserAgent,
                Locale = env.Languages?.Split(',')[0] ?? "zh-CN",
                Proxy = proxy,
                IgnoreHTTPSErrors = true
            });

            var page = await context.NewPageAsync();
            await page.AddInitScriptAsync(@"Object.defineProperty(navigator, 'webdriver', {get: () => false});");

            if (!string.IsNullOrEmpty(env.WebGLVendor))
            {
                await context.AddInitScriptAsync($@"
                    const originalGetContext = HTMLCanvasElement.prototype.getContext;
                    HTMLCanvasElement.prototype.getContext = function(type, options) {{
                        const ctx = originalGetContext.call(this, type, options);
                        if (type === 'webgl' || type === 'webgl2') {{
                            const vendor = '{env.WebGLVendor}';
                            const renderer = '{env.WebGLRenderer}';
                            if (ctx) {{
                                const getParameter = ctx.getParameter;
                                ctx.getParameter = function(param) {{
                                    if (param === 37445) return vendor;
                                    if (param === 37446) return renderer;
                                    return getParameter.call(this, param);
                                }};
                            }}
                        }}
                        return ctx;
                    }};
                ");
            }

            _runningBrowsers[env.Id] = browser;
            _contexts[env.Id] = context;
            _pages[env.Id] = page;
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }

    public async Task CloseBrowserAsync(int envId)
    {
        if (_pages.TryRemove(envId, out var page)) await page.CloseAsync();
        if (_contexts.TryRemove(envId, out var context)) await context.CloseAsync();
        if (_runningBrowsers.TryRemove(envId, out var browser)) await browser.CloseAsync();
        _semaphore.Release();
    }

    public async Task OpenUrl(int envId, string url)
    {
        if (_pages.TryGetValue(envId, out var page))
        {
            await page.BringToFrontAsync();
            await page.GotoAsync(url);
        }
    }

    private (int Width, int Height) ParseResolution(string? resolution)
    {
        if (string.IsNullOrEmpty(resolution)) return (1920, 1080);
        var parts = resolution.Split('x');
        if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
            return (w, h);
        return (1920, 1080);
    }

    public bool IsRunning(int envId) => _runningBrowsers.ContainsKey(envId);
}
