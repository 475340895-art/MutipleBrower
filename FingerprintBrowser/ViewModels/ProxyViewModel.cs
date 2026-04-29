using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.ViewModels
{
    public partial class ProxyViewModel : ObservableObject
    {
        private readonly BrowserDbContext _db;
        private readonly ProxyService _proxyService;

        [ObservableProperty]
        private ObservableCollection<ProxyConfig> _proxies = new();

        [ObservableProperty]
        private ProxyConfig? _selectedProxy;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public ProxyViewModel()
        {
            _db = new BrowserDbContext();
            _proxyService = new ProxyService(_db);
        }

        public async Task LoadProxiesAsync()
        {
            try
            {
                IsLoading = true;
                var proxies = await _db.Proxies.ToListAsync();
                Proxies.Clear();
                foreach (var proxy in proxies)
                {
                    Proxies.Add(proxy);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddProxyAsync(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            var proxy = new ProxyConfig
            {
                Name = name,
                Host = "127.0.0.1",
                Port = 8080,
                Protocol = "HTTP"
            };
            _db.Proxies.Add(proxy);
            await _db.SaveChangesAsync();
            Proxies.Add(proxy);
            StatusMessage = "代理已添加";
        }

        [RelayCommand]
        private async Task DeleteProxyAsync(ProxyConfig? proxy)
        {
            if (proxy == null) return;

            _db.Proxies.Remove(proxy);
            await _db.SaveChangesAsync();
            Proxies.Remove(proxy);
            StatusMessage = "代理已删除";
        }

        [RelayCommand]
        private async Task BatchTestAsync()
        {
            if (!Proxies.Any())
            {
                StatusMessage = "没有代理可测试";
                return;
            }

            IsLoading = true;
            StatusMessage = "正在批量测试代理...";

            var tasks = Proxies.Select(async p =>
            {
                var result = await _proxyService.TestProxyAsync(p);
                return (proxy: p, result);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (proxy, result) in results)
            {
                proxy.Status = result.IsValid ? ProxyStatus.Valid : ProxyStatus.Invalid;
                proxy.Latency = result.Latency;
                proxy.LastTestTime = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            IsLoading = false;
            StatusMessage = $"测试完成，有效: {results.Count(r => r.result.IsValid)}/{results.Length}";
        }

        [RelayCommand]
        private async Task DeleteSelectedAsync()
        {
            var selected = Proxies.Where(p => p.IsSelected).ToList();
            if (!selected.Any())
            {
                StatusMessage = "请先选择要删除的代理";
                return;
            }

            foreach (var proxy in selected)
            {
                _db.Proxies.Remove(proxy);
                Proxies.Remove(proxy);
            }
            await _db.SaveChangesAsync();
            StatusMessage = $"已删除 {selected.Count} 个代理";
        }

        [RelayCommand]
        private async Task BatchImportAsync(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            try
            {
                var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var count = 0;

                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(':');
                    if (parts.Length >= 2)
                    {
                        var proxy = new ProxyConfig
                        {
                            Name = $"代理_{count + 1}",
                            Host = parts[0],
                            Port = int.TryParse(parts[1], out var port) ? port : 0
                        };
                        _db.Proxies.Add(proxy);
                        Proxies.Add(proxy);
                        count++;
                    }
                }

                await _db.SaveChangesAsync();
                StatusMessage = $"成功导入 {count} 个代理";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导入失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task TestSingleProxyAsync(ProxyConfig? proxy)
        {
            if (proxy == null) return;

            StatusMessage = $"正在测试 {proxy.Host}:{proxy.Port}...";
            var result = await _proxyService.TestProxyAsync(proxy);
            proxy.Status = result.IsValid ? ProxyStatus.Valid : ProxyStatus.Invalid;
            proxy.Latency = result.Latency;
            proxy.LastTestTime = DateTime.Now;
            await _db.SaveChangesAsync();
            StatusMessage = result.IsValid
                ? $"代理有效，延迟: {result.Latency}ms"
                : "代理无效";
        }
    }
}
