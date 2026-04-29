using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Data;

/// <summary>
/// 浏览器数据库上下文
/// </summary>
public class BrowserDbContext : DbContext
{
    private static readonly string DbPath = Path.Combine(
        AppConstants.DefaultDataPath, "browser.db");

    public DbSet<BrowserEnvironment> Environments { get; set; } = null!;
    public DbSet<EnvironmentGroup> Groups { get; set; } = null!;
    public DbSet<ProxyConfig> Proxies { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var directory = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 环境配置
        modelBuilder.Entity<BrowserEnvironment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.ProxyConfig).HasMaxLength(500);
            entity.Property(e => e.StartupUrl).HasMaxLength(1000);
            
            entity.HasOne(e => e.Group)
                  .WithMany(g => g.Environments)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            entity.HasOne(e => e.Proxy)
                  .WithMany()
                  .HasForeignKey(e => e.ProxyId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.GroupId);
        });

        // 分组配置
        modelBuilder.Entity<EnvironmentGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.HasIndex(e => e.SortOrder);
        });

        // 代理配置
        modelBuilder.Entity<ProxyConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Host).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(200);
            entity.HasIndex(e => e.Host);
        });
    }

    public static void EnsureCreated()
    {
        using var context = new BrowserDbContext();
        context.Database.EnsureCreated();
    }
}
