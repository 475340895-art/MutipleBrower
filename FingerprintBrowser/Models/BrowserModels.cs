using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FingerprintBrowser.Models
{
    public class BrowserEnvironment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? GroupName { get; set; }

        public BrowserStatus Status { get; set; } = BrowserStatus.Stopped;

        public string OS { get; set; } = "Windows";

        public string? ProxyHost { get; set; }

        public int? ProxyPort { get; set; }

        public string? ProxyUsername { get; set; }

        public string? ProxyPassword { get; set; }

        public ProxyType ProxyType { get; set; } = ProxyType.None;

        public ProxyStatus ProxyStatus { get; set; } = ProxyStatus.Untested;

        public string? UserAgent { get; set; }

        public string Resolution { get; set; } = "1920x1080";

        public string? WebGLVendor { get; set; }

        public string? WebGLRenderer { get; set; }

        public bool EnableWebRTC { get; set; } = true;

        public bool EnableJavaScript { get; set; } = true;

        public bool EnableCookies { get; set; } = true;

        public string? Languages { get; set; }

        public string? Timezone { get; set; }

        public string? CanvasNoise { get; set; }

        public bool EnableAudioContext { get; set; } = true;

        public string? RunningBrowserId { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastOpenedAt { get; set; }

        [NotMapped]
        public string ProxyDisplay
        {
            get
            {
                if (ProxyType == ProxyType.None) return "无代理";
                return $"{ProxyType.ToString().ToLower()}://{ProxyHost}:{ProxyPort}";
            }
        }

        [NotMapped]
        public string StatusText => Status switch
        {
            BrowserStatus.Running => "运行中",
            BrowserStatus.Stopped => "已停止",
            _ => "未知"
        };

        [NotMapped]
        public string AvatarLetter => string.IsNullOrEmpty(Name) ? "?" : Name[..1].ToUpper();
    }

    public class EnvironmentGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Color { get; set; } = "#1677ff";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public int EnvironmentCount { get; set; }
    }

    public class ProxyConfigModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public ProxyType Type { get; set; } = ProxyType.HTTP;

        [Required]
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public ProxyStatus Status { get; set; } = ProxyStatus.Untested;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastTestedAt { get; set; }

        [NotMapped]
        public string TypeDisplay => Type.ToString();

        [NotMapped]
        public string StatusDisplay => Status switch
        {
            ProxyStatus.Normal => "正常",
            ProxyStatus.Failed => "失败",
            ProxyStatus.Testing => "测试中",
            ProxyStatus.Untested => "未测试",
            _ => "未知"
        };
    }

    public enum BrowserStatus
    {
        Idle = 0,
        Starting = 1,
        Running = 2,
        Stopped = 3,
        Error = 4
    }

    public enum ProxyStatus
    {
        Untested = 0,
        Normal = 1,
        Failed = 2,
        Testing = 3
    }

    public enum ProxyType
    {
        None = 0,
        HTTP = 1,
        HTTPS = 2,
        SOCKS5 = 3,
        SS = 4
    }
}
