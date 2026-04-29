using System.Collections.ObjectModel;
using System.Net.Sockets;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;

namespace FingerprintBrowser.ViewModels;

public class EnvironmentEditViewModel
{
    public BrowserEnvironment Environment { get; set; }
    public ObservableCollection<string> Resolutions { get; set; } = new()
    {
        "1920x1080", "1366x768", "1536x864", "1440x900", "1280x720", "2560x1440", "3840x2160"
    };
    public ObservableCollection<string> Timezones { get; set; } = new()
    {
        "Asia/Shanghai", "America/New_York", "Europe/London", "Asia/Tokyo", "Asia/Singapore"
    };
    public ObservableCollection<string> Languages { get; set; } = new()
    {
        "zh-CN,zh", "en-US,en", "ja-JP,ja", "ko-KR,ko", "es-ES,es"
    };
    public ObservableCollection<ProxyConfig> Proxies { get; set; } = new();
    public bool IsNew => Environment.Id == 0;
    public string Title => IsNew ? "新建环境" : "编辑环境";

    private readonly BrowserDbContext _db;

    public EnvironmentEditViewModel(BrowserEnvironment? env)
    {
        Environment = env ?? new BrowserEnvironment();
        _db = new BrowserDbContext();
        LoadProxies();
    }

    private void LoadProxies()
    {
        var proxies = _db.Proxies.ToList();
        Proxies.Clear();
        Proxies.Add(new ProxyConfig { Id = 0, Name = "无代理" });
        foreach (var p in proxies) Proxies.Add(p);
    }

    public void GenerateUserAgent()
    {
        var agents = new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0"
        };
        Environment.UserAgent = agents[new Random().Next(agents.Length)];
    }

    public void GenerateFingerprint()
    {
        var vendors = new[] { "Intel Inc.", "NVIDIA Corporation", "AMD" };
        var renderers = new[] { "Intel Iris OpenGL Engine", "NVIDIA GeForce GTX 1080", "AMD Radeon Pro 5500M" };
        
        Environment.WebGLVendor = vendors[new Random().Next(vendors.Length)];
        Environment.WebGLRenderer = renderers[new Random().Next(renderers.Length)];
        Environment.Resolution = Resolutions[new Random().Next(Resolutions.Count)];
        Environment.Timezone = Timezones[new Random().Next(Timezones.Count)];
    }

    public async Task TestProxyAsync()
    {
        if (Environment.ProxyConfig == null || string.IsNullOrEmpty(Environment.ProxyConfig.Host)) return;

        try
        {
            using var client = new TcpClient();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await client.ConnectAsync(Environment.ProxyConfig.Host, Environment.ProxyConfig.Port);
            stopwatch.Stop();
            System.Windows.MessageBox.Show($"代理可用，延迟: {stopwatch.ElapsedMilliseconds}ms", "测试结果");
        }
        catch
        {
            System.Windows.MessageBox.Show("代理连接失败", "测试结果", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    public async Task SaveAsync()
    {
        if (IsNew)
        {
            _db.Environments.Add(Environment);
        }
        _db.SaveChanges();
    }
}
