using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Services;

/// <summary>
/// 浏览器环境服务
/// </summary>
public class BrowserService
{
    private readonly PlaywrightService _playwrightService;

    public BrowserService()
    {
        _playwrightService = new PlaywrightService();
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
                .OrderByDescending(e => e.CreatedAt)
                .ToList();

            // 填充UI显示用字段
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
            
            return environment;
        });
    }

    public void CreateEnvironment(BrowserEnvironment environment)
    {
        CreateEnvironmentAsync(environment).GetAwaiter().GetResult();
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
                existing.UseProxy = environment.UseProxy;
                existing.ProxyId = environment.ProxyId;
                existing.ProxyConfig = environment.ProxyConfig;
                existing.BrowserType = environment.BrowserType;
                existing.UpdatedAt = DateTime.Now;

                context.SaveChanges();
            }
        });
    }

    public void UpdateEnvironment(BrowserEnvironment environment)
    {
        UpdateEnvironmentAsync(environment).GetAwaiter().GetResult();
    }

    public Task DeleteEnvironmentAsync(int id)
    {
        return Task.Run(async () =>
        {
            // 先停止浏览器
            await StopBrowserAsync(id);
            
            using var context = new BrowserDbContext();
            var env = context.Environments.Find(id);
            if (env != null)
            {
                context.Environments.Remove(env);
                context.SaveChanges();
            }
        });
    }

    public Task DeleteEnvironmentAsync(int environmentId)
    {
        return Task.Run(async () =>
        {
            await StopBrowserAsync(environmentId);
            using var context = new BrowserDbContext();
            var env = context.Environments.Find(environmentId);
            if (env != null)
            {
                context.Environments.Remove(env);
                context.SaveChanges();
            }
        });
    }

    public Task<BrowserEnvironment> CopyEnvironmentAsync(int id)
    {
        return Task.Run(async () =>
        {
            var original = await GetEnvironmentAsync(id);
            if (original == null)
            {
                throw new Exception("环境不存在");
            }

            var clone = original.Clone();
            clone.Id = 0;
            return await CreateEnvironmentAsync(clone);
        });
    }

    public Task CopyEnvironmentAsync(int environmentId)
    {
        return Task.Run(async () =>
        {
            await CopyEnvironmentAsync(environmentId);
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
                await _playwrightService.LaunchBrowserAsync(env);
                
                // 更新状态为运行中
                env.Status = 1; // Running
                env.LastRunTime = DateTime.Now;
                context.SaveChanges();
            }
            catch
            {
                // 更新状态为错误
                env.Status = 4; // Error
                context.SaveChanges();
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
                await _playwrightService.CloseBrowserAsync(environmentId);
                
                // 更新状态为空闲
                env.Status = 0; // Idle
                context.SaveChanges();
            }
            catch
            {
                env.Status = 0;
                context.SaveChanges();
            }
        });
    }

    public void OpenBrowserUrl(int environmentId)
    {
        _playwrightService.OpenUrl(environmentId);
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

    public Task ImportEnvironmentsAsync(string filePath)
    {
        return Task.Run(async () =>
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("导入文件不存在");
            }

            var json = await File.ReadAllTextAsync(filePath);
            var envs = JsonSerializer.Deserialize<List<BrowserEnvironment>>(json);
            
            if (envs == null || !envs.Any())
            {
                throw new Exception("导入文件中没有有效的环境数据");
            }

            using var context = new BrowserDbContext();
            
            foreach (var env in envs)
            {
                env.Id = 0; // 重置ID以创建新记录
                env.CreatedAt = DateTime.Now;
                env.UpdatedAt = DateTime.Now;
                context.Environments.Add(env);
            }
            
            await context.SaveChangesAsync();
        });
    }

    public Task ExportEnvironmentsAsync(string filePath)
    {
        return Task.Run(async () =>
        {
            var envs = await GetEnvironmentsAsync();
            var json = JsonSerializer.Serialize(envs, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(filePath, json);
        });
    }

    #endregion
}
