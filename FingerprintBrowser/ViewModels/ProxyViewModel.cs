using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;

namespace FingerprintBrowser.ViewModels
{
    public class ProxyViewModel
    {
        private readonly ProxyService _proxyService;

        public ProxyViewModel()
        {
            _proxyService = new ProxyService();
            Proxies = new ObservableCollection<ProxyConfig>();
        }

        public ObservableCollection<ProxyConfig> Proxies { get; private set; }

        public Task LoadProxiesAsync()
        {
            return Task.Run(async () =>
            {
                var proxies = await _proxyService.GetAllProxiesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Proxies = new ObservableCollection<ProxyConfig>(proxies);
                });
            });
        }

        public Task AddProxyAsync(ProxyConfig proxy)
        {
            return Task.Run(async () =>
            {
                await _proxyService.AddAsync(proxy);
            });
        }

        public Task DeleteProxyAsync(int proxyId)
        {
            return Task.Run(async () =>
            {
                await _proxyService.DeleteAsync(proxyId);
            });
        }

        public Task DeleteSelectedAsync()
        {
            return Task.Run(async () =>
            {
                var selected = Proxies.Where(p => p.IsSelected).Select(p => p.Id).ToList();
                foreach (var id in selected)
                {
                    await _proxyService.DeleteAsync(id);
                }
            });
        }

        public Task BatchImportAsync(string filePath)
        {
            return Task.Run(async () =>
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                var count = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var proxy = ParseProxyLine(line);
                    if (proxy != null)
                    {
                        await _proxyService.AddAsync(proxy);
                        count++;
                    }
                }

                await LoadProxiesAsync();
            });
        }

        public Task BatchTestAsync()
        {
            return Task.Run(async () =>
            {
                foreach (var proxy in Proxies)
                {
                    try
                    {
                        var result = await ProxyService.TestProxyAsync(proxy.ToString());
                        proxy.Status = result.Success ? "Available" : "Unavailable";
                        proxy.Latency = result.Latency;
                    }
                    catch
                    {
                        proxy.Status = "Unavailable";
                    }
                }
            });
        }

        private ProxyConfig? ParseProxyLine(string line)
        {
            try
            {
                // 支持格式: http://host:port 或 host:port:user:pass
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) return null;

                ProxyConfig proxy = new ProxyConfig
                {
                    CreatedAt = DateTime.Now
                };

                if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    proxy.Type = "HTTP";
                    trimmed = trimmed.Substring(7);
                }
                else if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    proxy.Type = "HTTPS";
                    trimmed = trimmed.Substring(8);
                }
                else if (trimmed.StartsWith("socks5://", StringComparison.OrdinalIgnoreCase))
                {
                    proxy.Type = "SOCKS5";
                    trimmed = trimmed.Substring(9);
                }

                var parts = trimmed.Split(':');
                if (parts.Length >= 2)
                {
                    proxy.Host = parts[0];
                    proxy.Port = int.Parse(parts[1]);

                    if (parts.Length >= 4)
                    {
                        proxy.Username = parts[2];
                        proxy.Password = parts[3];
                    }
                }

                return proxy;
            }
            catch
            {
                return null;
            }
        }
    }
}
