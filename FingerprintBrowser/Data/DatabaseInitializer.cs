using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FingerprintBrowser.Data;

/// <summary>
/// 数据库迁移和初始化辅助类
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// 确保数据库存在并应用迁移
    /// </summary>
    public static void Initialize()
    {
        try
        {
            Log.Information("开始初始化数据库...");

            using var context = new BrowserDbContext();

            // 确保数据库创建
            if (context.Database.EnsureCreated())
            {
                Log.Information("数据库已创建");
            }
            else
            {
                Log.Information("数据库已存在");
            }

            // 种子数据
            SeedData(context);

            Log.Information("数据库初始化完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 种子初始数据
    /// </summary>
    private static void SeedData(BrowserDbContext context)
    {
        // 检查是否已有分组数据
        if (context.Groups.Any())
        {
            return;
        }

        Log.Information("正在种子数据...");

        // 添加默认分组
        var groups = new List<EnvironmentGroup>
        {
            new() { Name = "工作账号", Color = "#3B82F6", SortOrder = 0 },
            new() { Name = "社交媒体", Color = "#10B981", SortOrder = 1 },
            new() { Name = "电商平台", Color = "#F59E0B", SortOrder = 2 },
            new() { Name = "测试环境", Color = "#EF4444", SortOrder = 3 }
        };

        context.Groups.AddRange(groups);
        context.SaveChanges();

        Log.Information("种子数据添加完成");
    }

    /// <summary>
    /// 备份数据库
    /// </summary>
    public static void Backup(string backupPath)
    {
        try
        {
            var dbPath = AppInfo.GetDatabasePath();

            if (!File.Exists(dbPath))
            {
                Log.Warning("数据库文件不存在，无法备份");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(backupPath) ?? string.Empty);
            File.Copy(dbPath, backupPath, overwrite: true);

            Log.Information("数据库已备份到: {Path}", backupPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库备份失败");
            throw;
        }
    }

    /// <summary>
    /// 恢复数据库
    /// </summary>
    public static void Restore(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                Log.Warning("备份文件不存在");
                return;
            }

            var dbPath = AppInfo.GetDatabasePath();

            // 关闭现有连接
            using var context = new BrowserDbContext();
            context.Database.CloseConnection();

            File.Copy(backupPath, dbPath, overwrite: true);

            Log.Information("数据库已从: {Path} 恢复", backupPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库恢复失败");
            throw;
        }
    }

    /// <summary>
    /// 获取数据库文件大小
    /// </summary>
    public static long GetDatabaseSize()
    {
        try
        {
            var dbPath = AppInfo.GetDatabasePath();
            return File.Exists(dbPath) ? new FileInfo(dbPath).Length : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 清理过期数据
    /// </summary>
    public static void CleanupOldData(int daysToKeep = 30)
    {
        try
        {
            using var context = new BrowserDbContext();

            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            // 清理从未使用的旧环境
            var oldEnvironments = context.Environments
                .Where(e => e.LastRunTime == null && e.CreatedAt < cutoffDate)
                .ToList();

            if (oldEnvironments.Any())
            {
                context.Environments.RemoveRange(oldEnvironments);
                context.SaveChanges();
                Log.Information("已清理 {Count} 个过期环境", oldEnvironments.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "清理过期数据失败");
        }
    }
}
