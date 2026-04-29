using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FingerprintBrowser.ViewModels;

public partial class ProxyViewModel : ObservableObject
{
    private readonly BrowserDbContext _db;
    private readonly ProxyService _proxyService;

    [ObservableProperty]
    private ObservableCollection<ProxyConfig> _proxies = new();

    [ObservableProperty]
    private ProxyConfig? _selectedProxy;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ProxyViewModel()
    {
        _db = new BrowserDbContext();
        _proxyService = new ProxyService();
    }

    public async Task LoadProxiesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载代理...";

            var list = await _db.Proxies.OrderBy(p => p.Name).ToListAsync();
            Proxies = new ObservableCollection<ProxyConfig>(list);

            StatusMessage = $"已加载 {list.Count} 个代理";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载代理失败");
            StatusMessage = "加载失败";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task AddProxyAsync(ProxyConfig proxy)
    {
        try
        {
            _db.Proxies.Add(proxy);
            await _db.SaveChangesAsync();
            await LoadProxiesAsync();
            StatusMessage = "代理添加成功";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "添加代理失败");
            StatusMessage = "添加失败";
        }
    }

    public async Task UpdateProxyAsync(ProxyConfig proxy)
    {
        try
        {
            _db.Proxies.Update(proxy);
            await _db.SaveChangesAsync();
            await LoadProxiesAsync();
            StatusMessage = "代理更新成功";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新代理失败");
            StatusMessage = "更新失败";
        }
    }

    public async Task DeleteProxyAsync(int id)
    {
        try
        {
            var proxy = await _db.Proxies.FindAsync(id);
            if (proxy != null)
            {
                _db.Proxies.Remove(proxy);
                await _db.SaveChangesAsync();
                await LoadProxiesAsync();
                StatusMessage = "代理删除成功";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除代理失败");
            StatusMessage = "删除失败";
        }
    }

    public async Task DeleteSelectedAsync(IEnumerable<int> ids)
    {
        try
        {
            var proxies = await _db.Proxies.Where(p => ids.Contains(p.Id)).ToListAsync();
            _db.Proxies.RemoveRange(proxies);
            await _db.SaveChangesAsync();
            await LoadProxiesAsync();
            StatusMessage = $"已删除 {proxies.Count} 个代理";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批量删除代理失败");
            StatusMessage = "批量删除失败";
        }
    }

    public async Task TestSingleProxyAsync(ProxyConfig proxy)
    {
        try
        {
            proxy.Status = ProxyStatus.Testing;
            var result = await _proxyService.TestProxyAsync(proxy);
            
            proxy.Status = result.Success ? ProxyStatus.Available : ProxyStatus.Unavailable;
            proxy.Latency = result.ResponseTime;
            proxy.LastTestedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            OnPropertyChanged(nameof(SelectedProxy));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "测试代理失败");
            proxy.Status = ProxyStatus.Unavailable;
        }
    }

    public async Task BatchTestAsync()
    {
        var proxies = Proxies.ToList();
        StatusMessage = $"正在测试 {proxies.Count} 个代理...";

        foreach (var proxy in proxies)
        {
            await TestSingleProxyAsync(proxy);
            await Task.Delay(100);
        }

        StatusMessage = "批量测试完成";
    }

    public async Task BatchImportAsync(string text)
    {
        try
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var count = 0;

            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length >= 2)
                {
                    var proxy = new ProxyConfig
                    {
                        Name = $"导入代理_{count + 1}",
                        Host = parts[0],
                        Port = int.TryParse(parts[1], out var port) ? port : 0,
                        Type = "HTTP"
                    };

                    _db.Proxies.Add(proxy);
                    count++;
                }
            }

            await _db.SaveChangesAsync();
            await LoadProxiesAsync();
            StatusMessage = $"成功导入 {count} 个代理";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批量导入代理失败");
            StatusMessage = "导入失败";
        }
    }
}
