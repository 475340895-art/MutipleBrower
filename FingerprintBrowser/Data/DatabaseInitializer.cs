using System;
using System.Linq;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Services;

/// <summary>
/// 数据库初始化器
/// </summary>
public static class DatabaseInitializer
{
    public static void Initialize()
    {
        try
        {
            // 确保数据库已创建
            BrowserDbContext.EnsureCreated();

            using var context = new BrowserDbContext();

            // 检查是否已有数据
            if (context.Groups.Any())
            {
                return; // 数据库已初始化
            }

            // 创建默认分组
            var defaultGroups = new[]
            {
                new EnvironmentGroup { Name = "默认分组", Color = "#3B82F6", SortOrder = 0 },
                new EnvironmentGroup { Name = "工作", Color = "#10B981", SortOrder = 1 },
                new EnvironmentGroup { Name = "社交媒体", Color = "#F59E0B", SortOrder = 2 },
                new EnvironmentGroup { Name = "电商", Color = "#EF4444", SortOrder = 3 },
                new EnvironmentGroup { Name = "其他", Color = "#8B5CF6", SortOrder = 4 }
            };

            context.Groups.AddRange(defaultGroups);
            context.SaveChanges();

            Console.WriteLine("数据库初始化完成，已创建默认分组。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"数据库初始化失败: {ex.Message}");
            throw;
        }
    }
}
