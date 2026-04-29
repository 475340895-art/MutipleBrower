using Microsoft.EntityFrameworkCore;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Data;

public class BrowserDbContext : DbContext
{
    public DbSet<BrowserEnvironment> Environments { get; set; } = null!;
    public DbSet<EnvironmentGroup> Groups { get; set; } = null!;
    public DbSet<ProxyConfigModel> Proxies { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(AppConstants.DataDirectory, AppConstants.DatabaseName);
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BrowserEnvironment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ProxyConfig).HasColumnType("TEXT");
        });

        modelBuilder.Entity<EnvironmentGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<ProxyConfigModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Host).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(20);
        });
    }
}
