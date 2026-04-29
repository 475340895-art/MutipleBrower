using System.Text.Json;
using System.Text.RegularExpressions;

namespace FingerprintBrowser.Services;

/// <summary>
/// 通用工具类
/// </summary>
public static class Utils
{
    /// <summary>
    /// 检查字符串是否为有效的 URL
    /// </summary>
    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// 检查字符串是否为有效的 IP 地址
    /// </summary>
    public static bool IsValidIpAddress(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;
        return System.Net.IPAddress.TryParse(ip, out _);
    }

    /// <summary>
    /// 检查字符串是否为有效的端口号
    /// </summary>
    public static bool IsValidPort(string? port)
    {
        if (string.IsNullOrWhiteSpace(port)) return false;
        return int.TryParse(port, out var p) && p > 0 && p <= 65535;
    }

    /// <summary>
    /// 安全地将对象序列化为 JSON
    /// </summary>
    public static string ToJson(object? obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// 安全地将 JSON 反序列化为对象
    /// </summary>
    public static T? FromJson<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 限制字符串长度
    /// </summary>
    public static string TruncateString(string? str, int maxLength)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;
        return str.Length <= maxLength ? str : str[..maxLength] + "...";
    }

    /// <summary>
    /// 从 URL 中提取域名
    /// </summary>
    public static string ExtractDomain(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 生成随机字符串
    /// </summary>
    public static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 格式化时间间隔
    /// </summary>
    public static string FormatTimeSpan(TimeSpan span)
    {
        if (span.TotalSeconds < 60) return $"{(int)span.TotalSeconds}秒";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}分钟";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}小时";
        return $"{(int)span.TotalDays}天";
    }

    /// <summary>
    /// 验证代理格式
    /// </summary>
    public static (bool IsValid, string? Error) ValidateProxy(string proxyType, string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            return (false, "主机地址不能为空");

        if (!IsValidIpAddress(host) && !IsValidDomain(host))
            return (false, "主机地址格式不正确");

        if (port <= 0 || port > 65535)
            return (false, "端口号必须在 1-65535 之间");

        var validTypes = new[] { "HTTP", "HTTPS", "SOCKS4", "SOCKS5" };
        if (!validTypes.Contains(proxyType.ToUpper()))
            return (false, "不支持的代理类型");

        return (true, null);
    }

    /// <summary>
    /// 验证域名格式
    /// </summary>
    public static bool IsValidDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return false;

        var regex = new Regex(@"^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$");
        return regex.IsMatch(domain);
    }

    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "untitled";

        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(sanitized) ? "untitled" : sanitized;
    }

    /// <summary>
    /// 解析 Cookie 字符串
    /// </summary>
    public static Dictionary<string, string> ParseCookieString(string cookieString)
    {
        var cookies = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(cookieString)) return cookies;

        try
        {
            var pairs = cookieString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    cookies[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }
        catch
        {
            // 忽略解析错误
        }

        return cookies;
    }
}
