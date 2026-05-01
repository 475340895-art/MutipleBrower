using System;
using System.Linq;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize(BrowserDbContext db)
        {
            db.Database.EnsureCreated();

            if (!db.Groups.Any())
            {
                db.Groups.AddRange(
                    new EnvironmentGroup { Name = "默认分组", Color = "#1677ff" },
                    new EnvironmentGroup { Name = "社交媒体", Color = "#52c41a" },
                    new EnvironmentGroup { Name = "电商运营", Color = "#fa8c16" },
                    new EnvironmentGroup { Name = "广告验证", Color = "#722ed1" },
                    new EnvironmentGroup { Name = "开发测试", Color = "#eb2f96" }
                );
                db.SaveChanges();
            }

            if (!db.Environments.Any())
            {
                var groups = db.Groups.ToList();
                var random = new Random();

                for (int i = 1; i <= 12; i++)
                {
                    var group = groups[random.Next(groups.Count)];
                    var env = new BrowserEnvironment
                    {
                        Name = $"账号-{i:D2}",
                        GroupName = group.Name,
                        Status = i <= 3 ? BrowserStatus.Running : BrowserStatus.Stopped,
                        OS = "Windows",
                        UserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.{random.Next(1000, 9999)}.0",
                        Resolution = "1920x1080",
                        WebGLVendor = "Google Inc. (NVIDIA)",
                        WebGLRenderer = "ANGLE (NVIDIA, GeForce GTX 1060)",
                        Languages = "en-US,en",
                        Timezone = "America/New_York",
                        CreatedAt = DateTime.Now.AddDays(-random.Next(1, 30)),
                        LastOpenedAt = DateTime.Now.AddHours(-random.Next(1, 48))
                    };

                    if (i <= 5)
                    {
                        env.ProxyType = i % 2 == 0 ? ProxyType.SOCKS5 : ProxyType.HTTP;
                        env.ProxyHost = $"10.0.{random.Next(1, 255)}.{random.Next(1, 255)}";
                        env.ProxyPort = 1080 + i;
                        env.ProxyStatus = i <= 2 ? ProxyStatus.Normal : ProxyStatus.Untested;
                    }
                    else
                    {
                        env.ProxyType = ProxyType.None;
                    }

                    db.Environments.Add(env);
                }
                db.SaveChanges();
            }

            if (!db.ProxyConfigs.Any())
            {
                db.ProxyConfigs.AddRange(
                    new ProxyConfigModel { Name = "美国-01", Type = ProxyType.SOCKS5, Host = "10.0.1.100", Port = 1080, Status = ProxyStatus.Normal },
                    new ProxyConfigModel { Name = "日本-01", Type = ProxyType.HTTP, Host = "10.0.2.200", Port = 8080, Status = ProxyStatus.Normal },
                    new ProxyConfigModel { Name = "德国-01", Type = ProxyType.HTTPS, Host = "10.0.3.50", Port = 3128, Status = ProxyStatus.Failed },
                    new ProxyConfigModel { Name = "新加坡-01", Type = ProxyType.SOCKS5, Host = "10.0.4.80", Port = 1081, Status = ProxyStatus.Untested }
                );
                db.SaveChanges();
            }
        }
    }
}
