using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FingerprintBrowser.Models;

public enum BrowserStatus
{
    Idle = 0,
    Starting = 1,
    Running = 2,
    Error = 3,
    Stopping = 4
}

public enum ProxyStatus
{
    Unknown = 0,
    Available = 1,
    Unavailable = 2,
    Testing = 3
}

public class BrowserEnvironment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? GroupId { get; set; }

    [ForeignKey("GroupId")]
    public EnvironmentGroup? Group { get; set; }

    // 浏览器指纹配置
    [MaxLength(50)]
    public string BrowserType { get; set; } = "Chromium";

    [MaxLength(50)]
    public string Resolution { get; set; } = "1920x1080";

    [MaxLength(100)]
    public string UserAgent { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Timezone { get; set; } = "Asia/Shanghai";

    [MaxLength(200)]
    public string Languages { get; set; } = "zh-CN,zh,en-US,en";

    public bool EnableWebRTC { get; set; } = true;
    public bool EnableCookies { get; set; } = true;
    public bool EnableJavaScript { get; set; } = true;

    [MaxLength(100)]
    public string WebGLVendor { get; set; } = string.Empty;

    [MaxLength(100)]
    public string WebGLRenderer { get; set; } = string.Empty;

    public int? ProxyId { get; set; }

    [ForeignKey("ProxyId")]
    public ProxyConfig? Proxy { get; set; }

    [MaxLength(500)]
    public string StartupUrl { get; set; } = "https://www.google.com";

    [MaxLength(500)]
    public string Remark { get; set; } = string.Empty;

    public BrowserStatus Status { get; set; } = BrowserStatus.Idle;

    [MaxLength(200)]
    public string StatusMessage { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 运行时的浏览器实例ID
    public int? RunningBrowserId { get; set; }

    // 代理信息（运行时使用）
    [NotMapped]
    public string? ProxyInfo => Proxy != null ? $"{Proxy.Host}:{Proxy.Port}" : null;
}

public class EnvironmentGroup
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Color { get; set; } = "#5B8DEF";

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<BrowserEnvironment> Environments { get; set; } = new List<BrowserEnvironment>();
}

public class ProxyConfig
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Type { get; set; } = "HTTP"; // HTTP, HTTPS, SOCKS5

    [Required]
    [MaxLength(200)]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(100)]
    public string? Password { get; set; }

    public ProxyStatus Status { get; set; } = ProxyStatus.Unknown;

    public int Latency { get; set; } = 0;

    [MaxLength(200)]
    public string Remark { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastTestedAt { get; set; }
}
