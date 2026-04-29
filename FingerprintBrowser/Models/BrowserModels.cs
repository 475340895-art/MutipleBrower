using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FingerprintBrowser.Models;

/// <summary>
/// 浏览器环境配置
/// </summary>
public class BrowserEnvironment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public EnvironmentGroup? Group { get; set; }

    // 状态: 0=未启动, 1=运行中, 2=异常
    public int Status { get; set; } = 0;

    public DateTime? LastRunTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 浏览器指纹配置
    public string? UserAgent { get; set; }

    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;

    public int TimeZone { get; set; } = 8; // 东八区
    public string? TimeZoneId { get; set; }

    public string? Language { get; set; } = "zh-CN";
    public string? Platform { get; set; } = "Win32";
    public string? HardwareConcurrency { get; set; } = "8";
    public string? DeviceMemory { get; set; } = "8";
    public string? WebGlVendor { get; set; }
    public string? WebGlRenderer { get; set; }

    // 代理配置
    public bool UseProxy { get; set; } = false;
    public int? ProxyId { get; set; }

    [ForeignKey(nameof(ProxyId))]
    public ProxyConfig? Proxy { get; set; }

    // 启动URL
    [MaxLength(1000)]
    public string? StartupUrl { get; set; }

    // Cookie导入
    public string? Cookies { get; set; }

    // 备注
    [MaxLength(500)]
    public string? Remarks { get; set; }

    // 是否收藏
    public bool IsFavorite { get; set; } = false;

    // 运行时信息（不存入数据库）
    [NotMapped]
    public string? RunningUrl { get; set; }

    [NotMapped]
    public int? ProcessId { get; set; }

    public BrowserEnvironment Clone()
    {
        return new BrowserEnvironment
        {
            Name = Name + " (副本)",
            Description = Description,
            GroupId = GroupId,
            UserAgent = UserAgent,
            ScreenWidth = ScreenWidth,
            ScreenHeight = ScreenHeight,
            TimeZone = TimeZone,
            TimeZoneId = TimeZoneId,
            Language = Language,
            Platform = Platform,
            HardwareConcurrency = HardwareConcurrency,
            DeviceMemory = DeviceMemory,
            WebGlVendor = WebGlVendor,
            WebGlRenderer = WebGlRenderer,
            UseProxy = UseProxy,
            ProxyId = ProxyId,
            StartupUrl = StartupUrl,
            Cookies = Cookies,
            Remarks = Remarks
        };
    }
}

/// <summary>
/// 环境分组
/// </summary>
public class EnvironmentGroup
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(7)]
    public string Color { get; set; } = "#3B82F6"; // 默认蓝色

    public int SortOrder { get; set; } = 0;

    public bool IsExpanded { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<BrowserEnvironment> Environments { get; set; } = new List<BrowserEnvironment>();

    // 运行中的环境数量（不存入数据库）
    [NotMapped]
    public int RunningCount => Environments?.Count(e => e.Status == 1) ?? 0;

    // 统计信息（不存入数据库）
    [NotMapped]
    public int TotalCount => Environments?.Count ?? 0;
}

/// <summary>
/// 代理配置
/// </summary>
public class ProxyConfig
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    // 代理类型: HTTP, HTTPS, SOCKS4, SOCKS5
    [Required]
    [MaxLength(20)]
    public string ProxyType { get; set; } = "HTTP";

    [Required]
    [MaxLength(200)]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(100)]
    public string? Password { get; set; }

    // 代理状态: 0=正常, 1=测试中, 2=不可用
    public int Status { get; set; } = 0;

    public DateTime? LastTestTime { get; set; }

    public int? ResponseTime { get; set; } // 响应时间(毫秒)

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<BrowserEnvironment> Environments { get; set; } = new List<BrowserEnvironment>();

    /// <summary>
    /// 获取代理URL
    /// </summary>
    public string GetProxyUrl()
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            return $"{ProxyType.ToLower()}://{Host}:{Port}";
        }
        return $"{ProxyType.ToLower()}://{Username}:{Password}@{Host}:{Port}";
    }
}

/// <summary>
/// 浏览器状态枚举
/// </summary>
public enum BrowserStatus
{
    Stopped = 0,
    Running = 1,
    Error = 2
}

/// <summary>
/// 代理状态枚举
/// </summary>
public enum ProxyStatus
{
    Normal = 0,
    Testing = 1,
    Unavailable = 2
}
