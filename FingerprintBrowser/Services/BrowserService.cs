using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FingerprintBrowser.Services;

/// <summary>
/// 浏览器服务 - 管理浏览器实例
/// </summary>
public interface IBrowserService : IDisposable
{
    /// <summary>
    /// 启动浏览器环境
    /// </summary>
    Task<bool> StartEnvironmentAsync(BrowserEnvironment environment);

    /// <summary>
    /// 停止浏览器环境
    /// </summary>
    Task<bool> StopEnvironmentAsync(int environmentId);

    /// <summary>
    /// 批量启动环境
    /// </summary>
    Task StartEnvironmentsAsync(IEnumerable<int> environmentIds);

    /// <summary>
    /// 批量停止环境
    /// </summary>
    Task StopEnvironmentsAsync(IEnumerable<int> environmentIds);

    /// <summary>
    /// 获取环境状态
    /// </summary>
    BrowserStatus GetEnvironmentStatus(int environmentId);

    /// <summary>
    /// 获取运行中的浏览器数量
    /// </summary>
    int GetRunningCount();
}

/// <summary>
/// 浏览器服务实现
/// </summary>
public class BrowserService : IBrowserService
{
    private readonly Dictionary<int, IBrowserWrapper> _runningBrowsers = new();
    private readonly object _lock = new();

    public async Task<bool> StartEnvironmentAsync(BrowserEnvironment environment)
    {
        if (environment == null)
        {
            Log.Warning("启动环境失败: 环境为空");
            return false;
        }

        try
        {
            Log.Information("正在启动环境: {Name} (ID: {Id})", environment.Name, environment.Id);

            // 检查是否已经在运行
            if (_runningBrowsers.ContainsKey(environment.Id))
            {
                Log.Warning("环境 {Name} 已经在运行中", environment.Name);
                return false;
            }

            // 创建浏览器实例
            var browser = await PlaywrightService.Instance.CreateBrowserAsync(environment);

            if (browser != null)
            {
                lock (_lock)
                {
                    _runningBrowsers[environment.Id] = browser;
                }

                // 更新数据库状态
                await UpdateEnvironmentStatusAsync(environment.Id, BrowserStatus.Running);

                Log.Information("环境 {Name} 启动成功", environment.Name);
                return true;
            }

            Log.Error("环境 {Name} 启动失败", environment.Name);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动环境 {Name} 时发生异常", environment.Name);
            await UpdateEnvironmentStatusAsync(environment.Id, BrowserStatus.Error);
            return false;
        }
    }

    public async Task<bool> StopEnvironmentAsync(int environmentId)
    {
        try
        {
            IBrowserWrapper? browser;
            lock (_lock)
            {
                _runningBrowsers.TryGetValue(environmentId, out browser);
            }

            if (browser != null)
            {
                await browser.CloseAsync();

                lock (_lock)
                {
                    _runningBrowsers.Remove(environmentId);
                }

                await UpdateEnvironmentStatusAsync(environmentId, BrowserStatus.Stopped);

                Log.Information("环境 ID {Id} 已停止", environmentId);
                return true;
            }

            Log.Warning("环境 ID {Id} 未在运行", environmentId);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "停止环境 ID {Id} 时发生异常", environmentId);
            return false;
        }
    }

    public async Task StartEnvironmentsAsync(IEnumerable<int> environmentIds)
    {
        var tasks = environmentIds.Select(id => StartSingleEnvironmentAsync(id));
        await Task.WhenAll(tasks);
    }

    private async Task StartSingleEnvironmentAsync(int environmentId)
    {
        try
        {
            using var context = new BrowserDbContext();
            var environment = await context.Environments
                .Include(e => e.Proxy)
                .FirstOrDefaultAsync(e => e.Id == environmentId);

            if (environment != null)
            {
                await StartEnvironmentAsync(environment);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动环境 ID {Id} 时发生异常", environmentId);
        }
    }

    public async Task StopEnvironmentsAsync(IEnumerable<int> environmentIds)
    {
        var tasks = environmentIds.Select(StopEnvironmentAsync);
        await Task.WhenAll(tasks);
    }

    public BrowserStatus GetEnvironmentStatus(int environmentId)
    {
        lock (_lock)
        {
            return _runningBrowsers.ContainsKey(environmentId)
                ? BrowserStatus.Running
                : BrowserStatus.Stopped;
        }
    }

    public int GetRunningCount()
    {
        lock (_lock)
        {
            return _runningBrowsers.Count;
        }
    }

    private static async Task UpdateEnvironmentStatusAsync(int environmentId, BrowserStatus status)
    {
        try
        {
            using var context = new BrowserDbContext();
            var environment = await context.Environments.FindAsync(environmentId);
            if (environment != null)
            {
                environment.Status = (int)status;
                if (status == BrowserStatus.Running)
                {
                    environment.LastRunTime = DateTime.Now;
                }
                environment.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新环境状态失败: {Id}", environmentId);
        }
    }

    public void Dispose()
    {
        Log.Information("正在关闭所有浏览器实例...");

        lock (_lock)
        {
            foreach (var browser in _runningBrowsers.Values)
            {
                try
                {
                    browser?.CloseAsync().Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "关闭浏览器时发生异常");
                }
            }
            _runningBrowsers.Clear();
        }

        Log.Information("所有浏览器实例已关闭");
    }
}
