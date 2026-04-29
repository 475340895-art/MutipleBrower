using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FingerprintBrowser.Services;

/// <summary>
/// 代理服务接口
/// </summary>
public interface IProxyService
{
    /// <summary>
    /// 获取所有代理
    /// </summary>
    Task<List<ProxyConfig>> GetAllProxiesAsync();

    /// <summary>
    /// 获取代理
    /// </summary>
    Task<ProxyConfig?> GetProxyAsync(int id);

    /// <summary>
    /// 添加代理
    /// </summary>
    Task<ProxyConfig> AddProxyAsync(ProxyConfig proxy);

    /// <summary>
    /// 更新代理
    /// </summary>
    Task<bool> UpdateProxyAsync(ProxyConfig proxy);

    /// <summary>
    /// 删除代理
    /// </summary>
    Task<bool> DeleteProxyAsync(int id);

    /// <summary>
    /// 测试代理
    /// </summary>
    Task<(bool Success, int ResponseTime)> TestProxyAsync(ProxyConfig proxy);

    /// <summary>
    /// 批量测试代理
    /// </summary>
    Task TestAllProxiesAsync();
}

/// <summary>
/// 代理服务实现
/// </summary>
public class ProxyService : IProxyService
{
    public async Task<List<ProxyConfig>> GetAllProxiesAsync()
    {
        try
        {
            using var context = new BrowserDbContext();
            return await context.Proxies.OrderBy(p => p.Name).ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取代理列表失败");
            return new List<ProxyConfig>();
        }
    }

    public async Task<ProxyConfig?> GetProxyAsync(int id)
    {
        try
        {
            using var context = new BrowserDbContext();
            return await context.Proxies.FindAsync(id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取代理失败: {Id}", id);
            return null;
        }
    }

    public async Task<ProxyConfig> AddProxyAsync(ProxyConfig proxy)
    {
        try
        {
            using var context = new BrowserDbContext();
            proxy.CreatedAt = DateTime.Now;
            proxy.UpdatedAt = DateTime.Now;
            context.Proxies.Add(proxy);
            await context.SaveChangesAsync();

            Log.Information("添加代理成功: {Name} ({Host}:{Port})", proxy.Name, proxy.Host, proxy.Port);
            return proxy;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "添加代理失败");
            throw;
        }
    }

    public async Task<bool> UpdateProxyAsync(ProxyConfig proxy)
    {
        try
        {
            using var context = new BrowserDbContext();
            proxy.UpdatedAt = DateTime.Now;
            context.Proxies.Update(proxy);
            await context.SaveChangesAsync();

            Log.Information("更新代理成功: {Name}", proxy.Name);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新代理失败: {Id}", proxy.Id);
            return false;
        }
    }

    public async Task<bool> DeleteProxyAsync(int id)
    {
        try
        {
            using var context = new BrowserDbContext();
            var proxy = await context.Proxies.FindAsync(id);

            if (proxy != null)
            {
                context.Proxies.Remove(proxy);
                await context.SaveChangesAsync();
                Log.Information("删除代理成功: {Id}", id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除代理失败: {Id}", id);
            return false;
        }
    }

    public async Task<(bool Success, int ResponseTime)> TestProxyAsync(ProxyConfig proxy)
    {
        try
        {
            proxy.Status = (int)ProxyStatus.Testing;
            await UpdateProxyAsync(proxy);

            var (success, responseTime) = await PlaywrightService.TestProxyAsync(proxy.GetProxyUrl());

            proxy.Status = success ? (int)ProxyStatus.Normal : (int)ProxyStatus.Unavailable;
            proxy.LastTestTime = DateTime.Now;
            proxy.ResponseTime = responseTime;
            await UpdateProxyAsync(proxy);

            return (success, responseTime);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "测试代理失败: {Name}", proxy.Name);
            return (false, 0);
        }
    }

    public async Task TestAllProxiesAsync()
    {
        try
        {
            var proxies = await GetAllProxiesAsync();
            var tasks = proxies.Select(p => TestProxyAsync(p));
            await Task.WhenAll(tasks);

            Log.Information("批量测试代理完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批量测试代理失败");
        }
    }
}
