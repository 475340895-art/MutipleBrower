using FingerprintBrowser.Models;
using Microsoft.Playwright;
using Serilog;

namespace FingerprintBrowser.Services;

public interface IBrowserWrapper
{
    IBrowser Browser { get; }
    IBrowserContext Context { get; }
}

public class PlaywrightService
{
    private static PlaywrightService? _instance;
    private static readonly object _lock = new();
    private IPlaywright? _playwright;
    private bool _isInitialized;

    public static PlaywrightService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new PlaywrightService();
                }
            }
            return _instance;
        }
    }

    private PlaywrightService() { }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _isInitialized = true;
            Log.Information("Playwright 初始化成功");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Playwright 初始化失败");
            throw;
        }
    }

    public async Task<IBrowser?> LaunchBrowserAsync(BrowserEnvironment env)
    {
        if (_playwright == null)
        {
            await InitializeAsync();
        }

        try
        {
            var proxy = env.Proxy;
            Proxy? playwrightProxy = null;
            
            if (proxy != null && !string.IsNullOrEmpty(proxy.Host))
            {
                playwrightProxy = new Proxy
                {
                    Server = $"{proxy.Host}:{proxy.Port}",
                    Username = proxy.Username,
                    Password = proxy.Password
                };
            }

            var contextOptions = new BrowserNewContextOptions
            {
                ViewportSize = ParseResolution(env.Resolution),
                UserAgent = string.IsNullOrEmpty(env.UserAgent) ? null : env.UserAgent,
                Locale = env.Languages.Split(',')[0],
                TimezoneId = env.Timezone,
                Proxy = playwrightProxy,
                IgnoreHTTPSErrors = true
            };

            var context = await _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--no-sandbox",
                    "--disable-dev-shm-usage"
                }
            });

            var browserContext = await context.NewContextAsync(contextOptions);

            if (!string.IsNullOrEmpty(env.StartupUrl))
            {
                var page = await browserContext.NewPageAsync();
                await page.GotoAsync(env.StartupUrl);
            }

            Log.Information("浏览器启动成功: {Name}", env.Name);
            return context;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动浏览器失败: {Name}", env.Name);
            return null;
        }
    }

    public async Task CloseBrowserAsync(int environmentId)
    {
        try
        {
            Log.Information("关闭浏览器: {Id}", environmentId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "关闭浏览器失败: {Id}", environmentId);
        }
    }

    public async Task OpenUrl(int environmentId, string url)
    {
        try
        {
            Log.Information("打开URL: {Id} - {Url}", environmentId, url);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开URL失败: {Id} - {Url}", environmentId, url);
        }
    }

    private static ViewportSize? ParseResolution(string resolution)
    {
        if (string.IsNullOrEmpty(resolution)) return null;

        var parts = resolution.Split('x');
        if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
        {
            return new ViewportSize { Width = width, Height = height };
        }

        return new ViewportSize { Width = 1920, Height = 1080 };
    }
}
