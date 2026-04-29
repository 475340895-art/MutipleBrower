using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.Data;

/// <summary>
/// 浏览器数据库上下文
/// </summary>
public class BrowserDbContext : DbContext
{
    public DbSet<BrowserEnvironment> Environments { get; set; } = null!;
    public DbSet<EnvironmentGroup> Groups { get; set; } = null!;
    public DbSet<ProxyConfig> Proxies { get; set; } = null!;

    private readonly string _dbPath;

    public BrowserDbContext()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FingerprintBrowser");

        Directory.CreateDirectory(appDataPath);
        _dbPath = Path.Combine(appDataPath, "browser_data.db");
    }

    public BrowserDbContext(DbContextOptions<BrowserDbContext> options) : base(options)
    {
        _dbPath = string.Empty;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 环境表配置
        modelBuilder.Entity<BrowserEnvironment>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.GroupId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Environments)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Proxy)
                .WithMany(p => p.Environments)
                .HasForeignKey(e => e.ProxyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 分组表配置
        modelBuilder.Entity<EnvironmentGroup>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.SortOrder);
        });

        // 代理表配置
        modelBuilder.Entity<ProxyConfig>(entity =>
        {
            entity.HasIndex(e => e.Host);
            entity.HasIndex(e => e.Status);
        });

        // 初始化默认数据
        modelBuilder.Entity<EnvironmentGroup>().HasData(
            new EnvironmentGroup { Id = 1, Name = "默认分组", Color = "#3B82F6", SortOrder = 0 }
        );
    }

    /// <summary>
    /// 获取数据库文件路径
    /// </summary>
    public string GetDatabasePath() => _dbPath;
}
