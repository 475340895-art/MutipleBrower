using Microsoft.EntityFrameworkCore;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Data;

public class BrowserDbContext : DbContext
{
    public DbSet<BrowserEnvironment> BrowserEnvironments { get; set; } = null!;
    public DbSet<EnvironmentGroup> EnvironmentGroups { get; set; } = null!;
    public DbSet<ProxyConfig> ProxyConfigs { get; set; } = null!;

    public string DbPath { get; }

    public BrowserDbContext()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(folder, "FingerprintBrowser");
        Directory.CreateDirectory(appFolder);
        DbPath = Path.Combine(appFolder, "browser.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BrowserEnvironment>().ToTable("BrowserEnvironments");
        modelBuilder.Entity<EnvironmentGroup>().ToTable("EnvironmentGroups");
        modelBuilder.Entity<ProxyConfig>().ToTable("ProxyConfigs");
    }
}
