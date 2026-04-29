using System.Collections.ObjectModel;
using System.Net.Sockets;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;

namespace FingerprintBrowser.ViewModels;

public class ProxyViewModel
{
    public ObservableCollection<ProxyConfig> Proxies { get; set; } = new();
    public ProxyConfig? SelectedProxy { get; set; }
    public string SearchText { get; set; } = "";
    public bool IsLoading { get; set; }
    public string StatusMessage { get; set; } = "就绪";

    private readonly BrowserDbContext _db;

    public ProxyViewModel()
    {
        _db = new BrowserDbContext();
    }

    public async Task LoadProxiesAsync()
    {
        IsLoading = true;
        try
        {
            var proxies = await Task.Run(() => _db.Proxies.ToList());
            Proxies.Clear();
            foreach (var p in proxies) Proxies.Add(p);
            StatusMessage = $"已加载 {Proxies.Count} 个代理";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task AddProxyAsync(string name, string host, int port, string type, string? username, string? password, string? remark)
    {
        var proxy = new ProxyConfig
        {
            Name = name,
            Host = host,
            Port = port,
            Type = type,
            Username = username,
            Password = password,
            Remark = remark
        };
        _db.Proxies.Add(proxy);
        _db.SaveChanges();
        Proxies.Add(proxy);
        StatusMessage = "代理已添加";
    }

    public async Task DeleteProxyAsync(ProxyConfig proxy)
    {
        _db.Proxies.Remove(proxy);
        _db.SaveChanges();
        Proxies.Remove(proxy);
        StatusMessage = "代理已删除";
    }

    public async Task DeleteSelectedAsync()
    {
        if (SelectedProxy == null) return;
        await DeleteProxyAsync(SelectedProxy);
    }

    public async Task TestSingleProxyAsync(ProxyConfig proxy)
    {
        proxy.Status = ProxyStatus.Testing;
        StatusMessage = $"正在测试 {proxy.Host}:{proxy.Port}...";
        
        try
        {
            using var client = new TcpClient();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await client.ConnectAsync(proxy.Host, proxy.Port);
            stopwatch.Stop();
            
            proxy.Status = ProxyStatus.Available;
            proxy.Latency = (int)stopwatch.ElapsedMilliseconds;
            proxy.LastTestAt = DateTime.Now;
            StatusMessage = $"代理可用，延迟 {proxy.Latency}ms";
        }
        catch
        {
            proxy.Status = ProxyStatus.Unavailable;
            proxy.Latency = null;
            StatusMessage = "代理不可用";
        }
        
        _db.SaveChanges();
    }

    public async Task BatchTestAsync()
    {
        IsLoading = true;
        foreach (var proxy in Proxies)
        {
            await TestSingleProxyAsync(proxy);
            await Task.Delay(100);
        }
        IsLoading = false;
        StatusMessage = "批量测试完成";
    }

    public async Task BatchImportAsync(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int count = 0;
        
        foreach (var line in lines)
        {
            var parts = line.Trim().Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int port))
            {
                var proxy = new ProxyConfig { Name = parts[0], Host = parts[0], Port = port };
                _db.Proxies.Add(proxy);
                Proxies.Add(proxy);
                count++;
            }
        }
        
        _db.SaveChanges();
        StatusMessage = $"导入成功: {count} 个代理";
    }
}
