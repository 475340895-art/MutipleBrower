using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FingerprintBrowser.Services;

public class BrowserService
{
    private static BrowserService? _instance;
    private static readonly object _lock = new();

    private readonly Dictionary<int, IBrowser> _runningBrowsers = new();
    private readonly PlaywrightService _playwrightService;

    public static BrowserService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new BrowserService();
                }
            }
            return _instance;
        }
    }

    private BrowserService()
    {
        _playwrightService = PlaywrightService.Instance;
    }

    public async Task<BrowserEnvironment> CreateEnvironmentAsync(BrowserEnvironment env)
    {
        try
        {
            using var db = new BrowserDbContext();
            db.Environments.Add(env);
            await db.SaveChangesAsync();
            return env;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建环境失败");
            throw;
        }
    }

    public async Task<BrowserEnvironment?> GetEnvironmentAsync(int id)
    {
        using var db = new BrowserDbContext();
        return await db.Environments
            .Include(e => e.Group)
            .Include(e => e.Proxy)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<BrowserEnvironment>> GetAllEnvironmentsAsync()
    {
        using var db = new BrowserDbContext();
        return await db.Environments
            .Include(e => e.Group)
            .Include(e => e.Proxy)
            .ToListAsync();
    }

    public async Task<bool> UpdateEnvironmentAsync(BrowserEnvironment env)
    {
        try
        {
            using var db = new BrowserDbContext();
            db.Environments.Update(env);
            await db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新环境失败");
            return false;
        }
    }

    public async Task<bool> DeleteEnvironmentAsync(int id)
    {
        try
        {
            using var db = new BrowserDbContext();
            var env = await db.Environments.FindAsync(id);
            if (env == null) return false;

            if (_runningBrowsers.ContainsKey(id))
            {
                await CloseBrowserAsync(id);
            }

            db.Environments.Remove(env);
            await db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除环境失败");
            return false;
        }
    }

    public async Task<BrowserEnvironment> CopyEnvironmentAsync(int id)
    {
        using var db = new BrowserDbContext();
        var source = await db.Environments.FindAsync(id);
        if (source == null)
            throw new ArgumentException($"环境 {id} 不存在");

        var copy = new BrowserEnvironment
        {
            Name = source.Name + " (副本)",
            GroupId = source.GroupId,
            BrowserType = source.BrowserType,
            Resolution = source.Resolution,
            UserAgent = source.UserAgent,
            Timezone = source.Timezone,
            Languages = source.Languages,
            EnableWebRTC = source.EnableWebRTC,
            EnableCookies = source.EnableCookies,
            EnableJavaScript = source.EnableJavaScript,
            WebGLVendor = source.WebGLVendor,
            WebGLRenderer = source.WebGLRenderer,
            ProxyId = source.ProxyId,
            StartupUrl = source.StartupUrl,
            Remark = source.Remark,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        db.Environments.Add(copy);
        await db.SaveChangesAsync();
        return copy;
    }

    public async Task<bool> LaunchBrowserAsync(BrowserEnvironment env)
    {
        try
        {
            if (_runningBrowsers.ContainsKey(env.Id))
            {
                Log.Warning("浏览器已在运行: {Name}", env.Name);
                return true;
            }

            var browser = await _playwrightService.LaunchBrowserAsync(env);
            if (browser != null)
            {
                _runningBrowsers[env.Id] = browser;
                env.RunningBrowserId = env.Id;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动浏览器失败: {Name}", env.Name);
            return false;
        }
    }

    public async Task CloseBrowserAsync(int environmentId)
    {
        try
        {
            if (_runningBrowsers.TryGetValue(environmentId, out var browser))
            {
                await browser.CloseAsync();
                _runningBrowsers.Remove(environmentId);

                using var db = new BrowserDbContext();
                var env = await db.Environments.FindAsync(environmentId);
                if (env != null)
                {
                    env.RunningBrowserId = null;
                    await db.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "关闭浏览器失败: {Id}", environmentId);
        }
    }

    public async Task OpenUrlAsync(int environmentId, string url)
    {
        try
        {
            if (_runningBrowsers.TryGetValue(environmentId, out var browser))
            {
                var pages = await browser.Contexts[0].PagesAsync();
                if (pages.Count > 0)
                {
                    await pages[0].BringToFrontAsync();
                    await pages[0].GotoAsync(url);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开URL失败: {Id} - {Url}", environmentId, url);
        }
    }

    public bool IsBrowserRunning(int environmentId)
    {
        return _runningBrowsers.ContainsKey(environmentId);
    }

    public async Task CloseAllBrowsersAsync()
    {
        var ids = _runningBrowsers.Keys.ToList();
        foreach (var id in ids)
        {
            await CloseBrowserAsync(id);
        }
    }
}
