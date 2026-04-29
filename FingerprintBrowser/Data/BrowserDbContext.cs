using Microsoft.EntityFrameworkCore;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Data;

public class BrowserDbContext : DbContext
{
    public DbSet<BrowserEnvironment> Environments => Set<BrowserEnvironment>();
    public DbSet<EnvironmentGroup> Groups => Set<EnvironmentGroup>();
    public DbSet<ProxyConfigModel> Proxies => Set<ProxyConfigModel>();
    
    private readonly string _dbPath;
    
    public BrowserDbContext()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(folder, "FingerprintBrowser");
        Directory.CreateDirectory(appFolder);
        _dbPath = Path.Combine(appFolder, "browser.db");
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={_dbPath}");
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BrowserEnvironment>()
            .HasOne(e => e.Group)
            .WithMany(g => g.Environments)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
