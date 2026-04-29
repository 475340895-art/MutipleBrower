using System.ComponentModel.DataAnnotations;

namespace FingerprintBrowser.Models;

public enum BrowserStatus { Idle, Starting, Running, Stopping, Error }
public enum ProxyStatus { Unknown, Available, Unavailable, Testing }
public enum ProxyType { HTTP, HTTPS, SOCKS5 }

public class BrowserEnvironment
{
    [Key] public int Id { get; set; }
    [Required][MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(100)] public string? GroupName { get; set; }
    [MaxLength(500)] public string? UserAgent { get; set; }
    [MaxLength(50)] public string? Resolution { get; set; }
    [MaxLength(100)] public string? Timezone { get; set; }
    [MaxLength(100)] public string? Languages { get; set; }
    public bool EnableWebRTC { get; set; } = true;
    public bool EnableCookies { get; set; } = true;
    public bool EnableJavaScript { get; set; } = true;
    [MaxLength(200)] public string? WebGLVendor { get; set; }
    [MaxLength(200)] public string? WebGLRenderer { get; set; }
    [MaxLength(1000)] public string? Remark { get; set; }
    [MaxLength(50)] public string BrowserType { get; set; } = "Chromium";
    public ProxyConfig? ProxyConfig { get; set; }
    public BrowserStatus Status { get; set; } = BrowserStatus.Idle;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastUsedAt { get; set; }
    public string? ProxyInfo => ProxyConfig != null ? $"{ProxyConfig.Host}:{ProxyConfig.Port}" : "无代理";
}

public class ProxyConfig
{
    [Key] public int Id { get; set; }
    [Required][MaxLength(200)] public string Name { get; set; } = "";
    [Required][MaxLength(200)] public string Host { get; set; } = "";
    public int Port { get; set; }
    public string Type { get; set; } = "HTTP";
    [MaxLength(200)] public string? Username { get; set; }
    [MaxLength(200)] public string? Password { get; set; }
    [MaxLength(1000)] public string? Remark { get; set; }
    public ProxyStatus Status { get; set; } = ProxyStatus.Unknown;
    public int? Latency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastTestAt { get; set; }
}

public class EnvironmentGroup
{
    [Key] public int Id { get; set; }
    [Required][MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(7)] public string Color { get; set; } = "#1E88E5";
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
