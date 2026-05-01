using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.Data
{
    public class BrowserDbContext : DbContext
    {
        public DbSet<BrowserEnvironment> Environments { get; set; }
        public DbSet<EnvironmentGroup> Groups { get; set; }
        public DbSet<ProxyConfigModel> ProxyConfigs { get; set; }

        public BrowserDbContext() { }

        public BrowserDbContext(DbContextOptions<BrowserDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=fingerprintbrowser.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BrowserEnvironment>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.OS).HasDefaultValue("Windows");
                entity.Property(e => e.Resolution).HasDefaultValue("1920x1080");
            });

            modelBuilder.Entity<EnvironmentGroup>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Color).HasDefaultValue("#1677ff");
            });

            modelBuilder.Entity<ProxyConfigModel>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Host).IsRequired();
            });
        }
    }
}
