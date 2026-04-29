using System.IO;
using System.Text;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace FingerprintBrowser.Services;

/// <summary>
/// 导入导出服务接口
/// </summary>
public interface IImportExportService
{
    /// <summary>
    /// 导出环境到文件
    /// </summary>
    Task<string> ExportEnvironmentAsync(int environmentId, string filePath);

    /// <summary>
    /// 批量导出环境
    /// </summary>
    Task<string> ExportEnvironmentsAsync(IEnumerable<int> environmentIds, string filePath);

    /// <summary>
    /// 导入环境
    /// </summary>
    Task<int> ImportEnvironmentAsync(string filePath);

    /// <summary>
    /// 从文本导入Cookie
    /// </summary>
    Task<bool> ImportCookiesFromTextAsync(int environmentId, string cookieText);

    /// <summary>
    /// 导出Cookie到文本
    /// </summary>
    Task<string> ExportCookiesToTextAsync(int environmentId);

    /// <summary>
    /// 导入代理列表
    /// </summary>
    Task<int> ImportProxiesFromTextAsync(string text, string format);

    /// <summary>
    /// 导出代理列表
    /// </summary>
    Task<string> ExportProxiesToTextAsync(string format);
}

/// <summary>
/// 导入导出服务实现
/// </summary>
public class ImportExportService : IImportExportService
{
    public async Task<string> ExportEnvironmentAsync(int environmentId, string filePath)
    {
        try
        {
            using var context = new BrowserDbContext();
            var environment = await context.Environments
                .Include(e => e.Group)
                .Include(e => e.Proxy)
                .FirstOrDefaultAsync(e => e.Id == environmentId);

            if (environment == null)
            {
                throw new Exception("环境不存在");
            }

            var json = JsonConvert.SerializeObject(environment, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

            Log.Information("导出环境成功: {Name} -> {Path}", environment.Name, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导出环境失败: {Id}", environmentId);
            throw;
        }
    }

    public async Task<string> ExportEnvironmentsAsync(IEnumerable<int> environmentIds, string filePath)
    {
        try
        {
            using var context = new BrowserDbContext();
            var environments = await context.Environments
                .Where(e => environmentIds.Contains(e.Id))
                .ToListAsync();

            var json = JsonConvert.SerializeObject(environments, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

            Log.Information("批量导出环境成功: {Count} 个环境 -> {Path}", environments.Count, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批量导出环境失败");
            throw;
        }
    }

    public async Task<int> ImportEnvironmentAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var environment = JsonConvert.DeserializeObject<BrowserEnvironment>(json);

            if (environment == null)
            {
                throw new Exception("文件格式错误");
            }

            // 重置ID以便插入新记录
            environment.Id = 0;
            environment.CreatedAt = DateTime.Now;
            environment.UpdatedAt = DateTime.Now;

            using var context = new BrowserDbContext();

            // 如果指定了代理，需要查找对应的代理ID
            if (environment.ProxyId.HasValue)
            {
                var proxy = await context.Proxies.FindAsync(environment.ProxyId);
                if (proxy == null)
                {
                    environment.ProxyId = null;
                }
            }

            context.Environments.Add(environment);
            await context.SaveChangesAsync();

            Log.Information("导入环境成功: {Name}", environment.Name);
            return environment.Id;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导入环境失败: {Path}", filePath);
            throw;
        }
    }

    public async Task<bool> ImportCookiesFromTextAsync(int environmentId, string cookieText)
    {
        try
        {
            // 尝试解析Cookie文本（支持JSON格式）
            using var context = new BrowserDbContext();
            var environment = await context.Environments.FindAsync(environmentId);

            if (environment == null)
            {
                return false;
            }

            // 简单存储Cookie文本
            environment.Cookies = cookieText;
            environment.UpdatedAt = DateTime.Now;

            await context.SaveChangesAsync();

            Log.Information("导入Cookie成功: 环境 {Id}", environmentId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导入Cookie失败: 环境 {Id}", environmentId);
            return false;
        }
    }

    public async Task<string> ExportCookiesToTextAsync(int environmentId)
    {
        try
        {
            using var context = new BrowserDbContext();
            var environment = await context.Environments.FindAsync(environmentId);

            if (environment?.Cookies == null)
            {
                return string.Empty;
            }

            return environment.Cookies;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导出Cookie失败: 环境 {Id}", environmentId);
            return string.Empty;
        }
    }

    public async Task<int> ImportProxiesFromTextAsync(string text, string format)
    {
        try
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var count = 0;

            using var context = new BrowserDbContext();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                try
                {
                    var proxy = ParseProxyLine(trimmedLine, format);
                    if (proxy != null)
                    {
                        proxy.CreatedAt = DateTime.Now;
                        proxy.UpdatedAt = DateTime.Now;
                        context.Proxies.Add(proxy);
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("解析代理行失败: {Line} - {Error}", trimmedLine, ex.Message);
                }
            }

            await context.SaveChangesAsync();
            Log.Information("导入代理成功: {Count} 个", count);
            return count;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导入代理失败");
            throw;
        }
    }

    private static ProxyConfig? ParseProxyLine(string line, string format)
    {
        ProxyConfig? proxy = null;

        switch (format.ToLower())
        {
            case "host:port":
                var parts1 = line.Split(':');
                if (parts1.Length >= 2 && int.TryParse(parts1[1], out var port1))
                {
                    proxy = new ProxyConfig
                    {
                        Name = parts1[0],
                        Host = parts1[0],
                        Port = port1,
                        ProxyType = "HTTP"
                    };
                }
                break;

            case "protocol://host:port":
                var parts2 = line.Split("://");
                if (parts2.Length == 2)
                {
                    var hostParts = parts2[1].Split(':');
                    if (hostParts.Length >= 2 && int.TryParse(hostParts[1], out var port2))
                    {
                        proxy = new ProxyConfig
                        {
                            Name = $"{parts2[1]}",
                            Host = hostParts[0],
                            Port = port2,
                            ProxyType = parts2[0].ToUpper()
                        };
                    }
                }
                break;

            case "host:port:user:pass":
                var parts3 = line.Split(':');
                if (parts3.Length >= 4)
                {
                    if (int.TryParse(parts3[1], out var port3))
                    {
                        proxy = new ProxyConfig
                        {
                            Name = parts3[0],
                            Host = parts3[0],
                            Port = port3,
                            Username = parts3[2],
                            Password = parts3[3],
                            ProxyType = "HTTP"
                        };
                    }
                }
                break;

            default:
                // 尝试自动检测格式
                if (line.Contains("://"))
                {
                    return ParseProxyLine(line, "protocol://host:port");
                }
                else if (line.Count(c => c == ':') >= 3)
                {
                    return ParseProxyLine(line, "host:port:user:pass");
                }
                else if (line.Contains(':'))
                {
                    return ParseProxyLine(line, "host:port");
                }
                break;
        }

        return proxy;
    }

    public async Task<string> ExportProxiesToTextAsync(string format)
    {
        try
        {
            using var context = new BrowserDbContext();
            var proxies = await context.Proxies.ToListAsync();

            var sb = new StringBuilder();
            foreach (var proxy in proxies)
            {
                var line = format.ToLower() switch
                {
                    "host:port" => $"{proxy.Host}:{proxy.Port}",
                    "protocol://host:port" => $"{proxy.ProxyType.ToLower()}://{proxy.Host}:{proxy.Port}",
                    "host:port:user:pass" when !string.IsNullOrEmpty(proxy.Username)
                        => $"{proxy.Host}:{proxy.Port}:{proxy.Username}:{proxy.Password}",
                    _ => $"{proxy.Host}:{proxy.Port}"
                };
                sb.AppendLine(line);
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导出代理失败");
            throw;
        }
    }
}
