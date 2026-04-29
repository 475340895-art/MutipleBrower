using FingerprintBrowser.Models;
using Microsoft.Playwright;
using Serilog;

namespace FingerprintBrowser.Services;

/// <summary>
/// Playwright 浏览器接口
/// </summary>
public interface IBrowser : IAsyncDisposable
{
    IBrowserContext Context { get; }
    IPage? CurrentPage { get; }
    Task<IPage> OpenUrlAsync(string url);
    Task CloseAsync();
}

/// <summary>
/// Playwright 实现
/// </summary>
public class PlaywrightBrowser : IBrowser
{
    private readonly IBrowser _browser;
    private readonly IBrowserContext _context;
    private IPage? _currentPage;

    public IBrowserContext Context => _context;
    public IPage? CurrentPage => _currentPage;

    public PlaywrightBrowser(IBrowser browser, IBrowserContext context)
    {
        _browser = browser;
        _context = context;
    }

    public async Task<IPage> OpenUrlAsync(string url)
    {
        _currentPage = await _context.NewPageAsync();
        await _currentPage.GotoAsync(url);
        return _currentPage;
    }

    public async Task CloseAsync()
    {
        try
        {
            if (_currentPage != null)
            {
                await _currentPage.CloseAsync();
                _currentPage = null;
            }

            await _context.CloseAsync();
            await _browser.CloseAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "关闭 Playwright 浏览器时发生异常");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
    }
}

/// <summary>
/// Playwright 服务 - 管理 Playwright 实例和浏览器创建
/// </summary>
public class PlaywrightService
{
    private static readonly Lazy<PlaywrightService> _instance = new(() => new PlaywrightService());
    public static PlaywrightService Instance => _instance.Value;

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _isInstalled;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private PlaywrightService()
    {
    }

    /// <summary>
    /// 确保 Playwright 已安装
    /// </summary>
    public async Task EnsureInstalledAsync()
    {
        try
        {
            Log.Information("正在检查 Playwright 安装状态...");

            var playwrightPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ms-playwright");

            if (!Directory.Exists(playwrightPath))
            {
                Log.Information("正在安装 Playwright 浏览器驱动...");
                await Microsoft.Playwright.Program.Install(new string[] { "install", "chromium" });
                _isInstalled = true;
                Log.Information("Playwright 浏览器驱动安装完成");
            }
            else
            {
                _isInstalled = true;
                Log.Information("Playwright 已安装");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Playwright 安装失败");
            _isInstalled = false;
        }
    }

    /// <summary>
    /// 创建浏览器实例
    /// </summary>
    public async Task<IBrowser?> CreateBrowserAsync(BrowserEnvironment environment)
    {
        await _semaphore.WaitAsync();
        try
        {
            _playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();

            // 创建浏览器上下文（隔离环境）
            var contextOptions = CreateContextOptions(environment);
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--disable-dev-shm-usage",
                    "--no-sandbox"
                }
            });

            var context = await _browser.NewContextAsync(contextOptions);

            // 打开URL
            var browserImpl = new PlaywrightBrowser(_browser, context);
            if (!string.IsNullOrEmpty(environment.StartupUrl))
            {
                await browserImpl.OpenUrlAsync(environment.StartupUrl);
            }

            return browserImpl;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建浏览器实例失败");
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 创建浏览器上下文选项（指纹配置）
    /// </summary>
    private BrowserNewContextOptions CreateContextOptions(BrowserEnvironment env)
    {
        var options = new BrowserNewContextOptions
        {
            // 视口大小
            ViewportSize = new ViewportSize
            {
                Width = env.ScreenWidth,
                Height = env.ScreenHeight
            },

            // 用户代理
            UserAgent = env.UserAgent ?? GenerateUserAgent(),

            // 地理位置（如果需要）
            // Geolocation = new Geolocation { Longitude = 116.4, Latitude = 39.9 },

            // 权限
            Permissions = new[] { "geolocation", "notifications" },

            // 时区
            TimezoneId = env.TimeZoneId ?? GetTimezoneId(env.TimeZone),

            // 语言
            Locale = env.Language ?? "zh-CN",

            // 平台（注意：此参数可能不支持，但会尝试）
            // Platform = env.Platform
        };

        // WebGL 配置（通过启动参数）
        if (!string.IsNullOrEmpty(env.WebGlVendor) && !string.IsNullOrEmpty(env.WebGlRenderer))
        {
            // 这些通常通过浏览器启动参数设置
            // Playwright 可能需要自定义 CDP 会话来实现
        }

        return options;
    }

    /// <summary>
    /// 生成随机用户代理
    /// </summary>
    private static string GenerateUserAgent()
    {
        var chromeVersions = new[]
        {
            "119.0.6045.106",
            "120.0.6099.109",
            "121.0.6167.85"
        };

        var random = new Random();
        var version = chromeVersions[random.Next(chromeVersions.Length)];

        return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{version} Safari/537.36";
    }

    /// <summary>
    /// 根据时区偏移获取时区ID
    /// </summary>
    private static string GetTimezoneId(int offset)
    {
        return offset switch
        {
            -12 => "Etc/GMT+12",
            -11 => "Pacific/Samoa",
            -10 => "Pacific/Honolulu",
            -9 => "America/Anchorage",
            -8 => "America/Los_Angeles",
            -7 => "America/Denver",
            -6 => "America/Chicago",
            -5 => "America/New_York",
            -4 => "America/Halifax",
            -3 => "America/Sao_Paulo",
            -2 => "Atlantic/South_Georgia",
            -1 => "Atlantic/Azores",
            0 => "UTC",
            1 => "Europe/Paris",
            2 => "Europe/Helsinki",
            3 => "Europe/Moscow",
            4 => "Asia/Dubai",
            5 => "Asia/Karachi",
            6 => "Asia/Dhaka",
            7 => "Asia/Bangkok",
            8 => "Asia/Shanghai",
            9 => "Asia/Tokyo",
            10 => "Australia/Sydney",
            11 => "Pacific/Noumea",
            12 => "Pacific/Auckland",
            _ => "Asia/Shanghai"
        };
    }

    /// <summary>
    /// 测试代理连接
    /// </summary>
    public static async Task<(bool Success, int ResponseTime)> TestProxyAsync(string proxyUrl)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            var proxy = new Proxy
            {
                Server = proxyUrl
            };

            // 创建临时浏览器测试代理
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Proxy = proxy
            });

            await browser.CloseAsync();

            stopwatch.Stop();
            return (true, (int)stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "代理测试失败: {ProxyUrl}", proxyUrl);
            stopwatch.Stop();
            return (false, (int)stopwatch.ElapsedMilliseconds);
        }
    }
}
