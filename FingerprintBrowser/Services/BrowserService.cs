using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Serilog;

namespace FingerprintBrowser.Services;

/// <summary>
/// 浏览器环境服务
/// </summary>
public class BrowserService
{
    private readonly PlaywrightService _playwrightService;
    private readonly Dictionary<int, IBrowserWrapper> _runningBrowsers = new();

    public BrowserService()
    {
        _playwrightService = PlaywrightService.Instance;
    }

    #region 分组管理

    public Task<List<EnvironmentGroup>> GetGroupsAsync()
    {
        return Task.Run(() =>
        {
            using var context = new BrowserDbContext();
            var groups = context.Groups
                .Include(g => g.Environments)
                .OrderBy(g => g.SortOrder)
                .ToList();
            return groups;
        });
    }

    public Task<EnvironmentGroup> CreateGroupAsync(EnvironmentGroup group)
    {
        return Task.Run(() =>
        {
            using var context = new BrowserDbContext();
            group.CreatedAt = DateTime.Now;
            context.Groups.Add(group);
            context.SaveChanges();
            return group;
        });
    }

    public Task UpdateGroupAsync(EnvironmentGroup group)
    {
        return Task.Run(() =>
        {
            using var context = new BrowserDbContext();
            var existing = context.Groups.Find(group.Id);
            if (existing != null)
            {
                existing.Name = group.Name;
                existing.Color = group.Color;
                existing.SortOrder = group.SortOrder;
                context.SaveChanges();
            }
        });
    }

    public Task DeleteGroupAsync(int groupId)
    {
        return Task.Run(() =>
        {
            using var context = new BrowserDbContext();
            var group = context.Groups.Find(groupId);
            if (group != null)
            {
                context.Groups.Remove(group);
                context.SaveChanges();
            }
        });
    }

    #endregion

    #region 环境管理

    public Task<List<BrowserEnvironment>> GetEnvironmentsAsync()
    {
        return Task.Run(() =>
        {
            using var context = new BrowserDbContext();
            var envs = context.Environments
                .Include(e => e.Group)
                .Include(e => e.Proxy)
                .OrderBy(e => e.CreatedAt)
                .ToList();

            foreach (var env in envs)
            {
                env.GroupName = env.Group?.Name ?? "默认分组";
                env.ProxyInfo = !string.IsNullOrEmpty(env.ProxyConfig) 
                    ? env.ProxyConfig.Split(':').ElementAtOrDefault(1)?.TrimStart('/') ?? "已配置"
                    : "无代理";
            }

            return envs;
        });
    }

    public Task<BrowserEnvironment?> GetEnvironmentAsync(int id)
    {
        return Task.Run(() =>
        {
            using var context = new BrowserDbContext();
            var env = context.Environments
                .Include(e => e.Group)
                .Include(e => e.Proxy)
                .FirstOrDefault(e => e.Id == id);
            
            if (env != null)
            {
                env.GroupName = env.Group?.Name ?? "默认分组";
                env.ProxyInfo = !string.IsNullOrEmpty(env.ProxyConfig) 
                    ? env.ProxyConfig : "无代理";
            }
            
            return env;
        });
    }

    public Task<BrowserEnvironment> CreateEnvironmentAsync(BrowserEnvironment environment)
    {
        return Task.Run(() =>
        {
            using var context = new BrowserDbContext();
            
            // 确保有分组
            if (environment.GroupId == 0)
            {
                var defaultGroup = context.Groups.FirstOrDefault();
                environment.GroupId = defaultGroup?.Id ?? 1;
            }

            environment.CreatedAt = DateTime.Now;
            environment.UpdatedAt = DateTime.Now;
            
            // 生成随机指纹
            if (string.IsNullOrEmpty(environment.UserAgent))
            {
                environment.UserAgent = FingerprintGenerator.GenerateUserAgent();
            }

            context.Environments.Add(environment);
            context.SaveChanges();
            
            Log.Information("创建浏览器环境: {Name}", environment.Name);
            return environment;
        });
    }

    public Task UpdateEnvironmentAsync(BrowserEnvironment environment)
    {
        return Task.Run(() =>
        {
            using var context = new BrowserDbContext();
            var existing = context.Environments.Find(environment.Id);
            if (existing != null)
            {
                existing.Name = environment.Name;
                existing.GroupId = environment.GroupId;
                existing.Remark = environment.Remark;
                existing.StartupUrl = environment.StartupUrl;
                existing.UserAgent = environment.UserAgent;
                existing.ScreenWidth = environment.ScreenWidth;
                existing.ScreenHeight = environment.ScreenHeight;
                existing.Timezone = environment.Timezone;
                existing.Languages = environment.Languages;
                existing.WebglVendor = environment.WebglVendor;
                existing.WebglRenderer = environment.WebglRenderer;
                existing.BrowserType = environment.BrowserType;
                existing.UseProxy = environment.UseProxy;
                existing.ProxyId = environment.ProxyId;
                existing.ProxyConfig = environment.ProxyConfig;
                existing.UpdatedAt = DateTime.Now;
                context.SaveChanges();
            }
        });
    }

    public Task DeleteEnvironmentAsync(int environmentId)
    {
        return Task.Run(async () =>
        {
            // 先关闭浏览器
            if (_runningBrowsers.ContainsKey(environmentId))
            {
                await CloseBrowserInternalAsync(environmentId);
            }

            using var context = new BrowserDbContext();
            var env = context.Environments.Find(environmentId);
            if (env != null)
            {
                context.Environments.Remove(env);
                context.SaveChanges();
            }
        });
    }

    public Task<BrowserEnvironment> CopyEnvironmentAsync(int environmentId)
    {
        return Task.Run(() =>
        {
            using var context = new BrowserDbContext();
            var original = context.Environments.Find(environmentId);
            if (original == null)
                throw new Exception("环境不存在");

            var copy = new BrowserEnvironment
            {
                Name = original.Name + " (副本)",
                GroupId = original.GroupId,
                Remark = original.Remark,
                StartupUrl = original.StartupUrl,
                UserAgent = original.UserAgent,
                ScreenWidth = original.ScreenWidth,
                ScreenHeight = original.ScreenHeight,
                Timezone = original.Timezone,
                Languages = original.Languages,
                WebglVendor = original.WebglVendor,
                WebglRenderer = original.WebglRenderer,
                BrowserType = original.BrowserType,
                UseProxy = original.UseProxy,
                ProxyId = original.ProxyId,
                ProxyConfig = original.ProxyConfig,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            context.Environments.Add(copy);
            context.SaveChanges();
            
            Log.Information("复制浏览器环境: {Name}", copy.Name);
            return copy;
        });
    }

    #endregion

    #region 浏览器控制

    public Task StartBrowserAsync(int environmentId)
    {
        return Task.Run(async () =>
        {
            using var context = new BrowserDbContext();
            var env = context.Environments.Find(environmentId);
            if (env == null)
            {
                throw new Exception("环境不存在");
            }

            // 更新状态为启动中
            env.Status = 2; // Starting
            context.SaveChanges();

            try
            {
                // 创建浏览器实例
                var browserWrapper = await _playwrightService.CreateBrowserAsync(env);
                if (browserWrapper != null)
                {
                    _runningBrowsers[environmentId] = browserWrapper;
                    
                    // 打开启动URL
                    if (!string.IsNullOrEmpty(env.StartupUrl))
                    {
                        await browserWrapper.OpenUrlAsync(env.StartupUrl);
                    }
                }
                
                // 更新状态为运行中
                env.Status = 1; // Running
                env.LastRunTime = DateTime.Now;
                context.SaveChanges();
                
                Log.Information("启动浏览器环境: {Name}", env.Name);
            }
            catch (Exception ex)
            {
                // 更新状态为错误
                env.Status = 4; // Error
                context.SaveChanges();
                Log.Error(ex, "启动浏览器环境失败: {Name}", env.Name);
                throw;
            }
        });
    }

    public Task StopBrowserAsync(int environmentId)
    {
        return Task.Run(async () =>
        {
            using var context = new BrowserDbContext();
            var env = context.Environments.Find(environmentId);
            if (env == null) return;

            // 更新状态为停止中
            env.Status = 3; // Stopping
            context.SaveChanges();

            try
            {
                await CloseBrowserInternalAsync(environmentId);
                
                // 更新状态为空闲
                env.Status = 0; // Idle
                context.SaveChanges();
                
                Log.Information("关闭浏览器环境: {Name}", env.Name);
            }
            catch (Exception ex)
            {
                env.Status = 0;
                context.SaveChanges();
                Log.Error(ex, "关闭浏览器环境失败: {Name}", env.Name);
            }
        });
    }

    private async Task CloseBrowserInternalAsync(int environmentId)
    {
        if (_runningBrowsers.TryGetValue(environmentId, out var browser))
        {
            await browser.CloseAsync();
            _runningBrowsers.Remove(environmentId);
        }
    }

    public void OpenBrowserUrl(int environmentId)
    {
        if (_runningBrowsers.TryGetValue(environmentId, out var browser))
        {
            // 获取环境配置的启动URL并打开
            using var context = new BrowserDbContext();
            var env = context.Environments.Find(environmentId);
            if (env != null && !string.IsNullOrEmpty(env.StartupUrl))
            {
                _ = browser.OpenUrlAsync(env.StartupUrl);
            }
        }
    }

    public Task BatchStartAsync()
    {
        return Task.Run(async () =>
        {
            using var context = new BrowserDbContext();
            var envs = context.Environments.Where(e => e.Status == 0).Take(5).ToList();
            
            foreach (var env in envs)
            {
                try
                {
                    await StartBrowserAsync(env.Id);
                }
                catch
                {
                    // 忽略单个启动失败
                }
            }
        });
    }

    public Task BatchStopAsync()
    {
        return Task.Run(async () =>
        {
            using var context = new BrowserDbContext();
            var envs = context.Environments.Where(e => e.Status == 1).ToList();
            
            foreach (var env in envs)
            {
                try
                {
                    await StopBrowserAsync(env.Id);
                }
                catch
                {
                    // 忽略单个停止失败
                }
            }
        });
    }

    #endregion

    #region 导入导出

    public async Task ImportEnvironmentsAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var environments = JsonSerializer.Deserialize<List<BrowserEnvironment>>(json);
        
        if (environments == null) return;

        using var context = new BrowserDbContext();
        foreach (var env in environments)
        {
            env.Id = 0; // 重置ID
            env.CreatedAt = DateTime.Now;
            env.UpdatedAt = DateTime.Now;
            context.Environments.Add(env);
        }
        context.SaveChanges();
        
        Log.Information("导入 {Count} 个浏览器环境", environments.Count);
    }

    public async Task ExportEnvironmentsAsync(string filePath)
    {
        var envs = await GetEnvironmentsAsync();
        var json = JsonSerializer.Serialize(envs, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
        
        Log.Information("导出 {Count} 个浏览器环境", envs.Count);
    }

    #endregion
}
