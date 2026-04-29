using System.ComponentModel.DataAnnotations;

namespace FingerprintBrowser.Models;

public enum BrowserStatus { Idle = 0, Starting = 1, Running = 2, Stopped = 3, Error = 4 }
public enum ProxyStatus { Unknown = 0, Available = 1, Unavailable = 2, Testing = 3 }
public enum ProxyType { HTTP = 0, HTTPS = 1, SOCKS5 = 2 }

public class BrowserEnvironment
{
    [Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? GroupId { get; set; }
    public EnvironmentGroup? Group { get; set; }
    public string? StartupUrl { get; set; }
    public string? UserAgent { get; set; }
    public string? Resolution { get; set; }
    public string? Timezone { get; set; }
    public string? Languages { get; set; }
    public string? WebGLVendor { get; set; }
    public string? WebGLRenderer { get; set; }
    public bool EnableWebRTC { get; set; } = true;
    public bool EnableCookies { get; set; } = true;
    public bool EnableJavaScript { get; set; } = true;
    public string? ProxyConfig { get; set; }
    public string? Remark { get; set; }
    public BrowserStatus Status { get; set; } = BrowserStatus.Idle;
    public int? RunningBrowserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class EnvironmentGroup
{
    [Key] public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#667eea";
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<BrowserEnvironment> Environments { get; set; } = new List<BrowserEnvironment>();
}

public class ProxyConfigModel
{
    [Key] public int Id { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public ProxyType Type { get; set; } = ProxyType.HTTP;
    public string? Remark { get; set; }
    public ProxyStatus Status { get; set; } = ProxyStatus.Unknown;
    public int? Latency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
