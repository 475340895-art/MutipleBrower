using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FingerprintBrowser.Models
{
    /// <summary>
    /// 浏览器环境状态枚举
    /// </summary>
    public enum BrowserEnvironmentStatus
    {
        Idle = 0,           // 空闲
        Starting = 1,        // 启动中
        Running = 2,         // 运行中
        Stopping = 3,       // 停止中
        Error = 4            // 错误
    }

    /// <summary>
    /// 代理类型枚举
    /// </summary>
    public enum ProxyType
    {
        None = 0,
        HTTP = 1,
        HTTPS = 2,
        SOCKS5 = 3
    }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    public enum BrowserTypeEnum
    {
        Chromium = 0,
        Firefox = 1,
        WebKit = 2
    }

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

        public int GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public EnvironmentGroup? Group { get; set; }

        public int? ProxyId { get; set; }

        [ForeignKey(nameof(ProxyId))]
        public ProxyConfig? ProxyConfig { get; set; }

        // 核心配置
        [MaxLength(50)]
        public string BrowserType { get; set; } = "Chromium";

        [MaxLength(50)]
        public string Resolution { get; set; } = "1920x1080";

        [MaxLength(100)]
        public string Timezone { get; set; } = "Asia/Shanghai";

        [MaxLength(200)]
        public string Languages { get; set; } = "zh-CN,zh,en-US,en";

        [MaxLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        [MaxLength(50)]
        public string WebGLVendor { get; set; } = "Intel Inc.";

        [MaxLength(50)]
        public string WebGLRenderer { get; set; } = "Intel Iris OpenGL Engine";

        public bool EnableWebRTC { get; set; } = true;
        public bool EnableCookies { get; set; } = true;
        public bool EnableJavaScript { get; set; } = true;

        // 启动URL
        [MaxLength(1000)]
        public string StartupUrl { get; set; } = "https://www.google.com";

        // 备注
        [MaxLength(500)]
        public string Remark { get; set; } = string.Empty;

        // 状态
        public BrowserEnvironmentStatus Status { get; set; } = BrowserEnvironmentStatus.Idle;
        public DateTime? LastRunTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 运行时的浏览器实例ID
        [NotMapped]
        public int? RunningBrowserId { get; set; }

        // 计算属性
        [NotMapped]
        public string StatusText => Status switch
        {
            BrowserEnvironmentStatus.Idle => "空闲",
            BrowserEnvironmentStatus.Starting => "启动中",
            BrowserEnvironmentStatus.Running => "运行中",
            BrowserEnvironmentStatus.Stopping => "停止中",
            BrowserEnvironmentStatus.Error => "错误",
            _ => "未知"
        };

        [NotMapped]
        public string GroupName => Group?.Name ?? "未分组";

        [NotMapped]
        public string ProxyInfo => ProxyConfig != null
            ? $"{ProxyConfig.Host}:{ProxyConfig.Port}"
            : "无代理";

        [NotMapped]
        public string DisplayResolution => Resolution;
    }

    /// <summary>
    /// 环境分组
    /// </summary>
    public class EnvironmentGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Color { get; set; } = "#1890ff";

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<BrowserEnvironment> Environments { get; set; } = new List<BrowserEnvironment>();
    }

    /// <summary>
    /// 代理配置
    /// </summary>
    public class ProxyConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        [MaxLength(50)]
        public string Protocol { get; set; } = "HTTP";

        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Remark { get; set; } = string.Empty;

        // 状态
        public ProxyStatus Status { get; set; } = ProxyStatus.Untested;
        public int? Latency { get; set; }
        public DateTime? LastTestTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 关联的环境
        public ICollection<BrowserEnvironment> Environments { get; set; } = new List<BrowserEnvironment>();
    }

    /// <summary>
    /// 代理状态
    /// </summary>
    public enum ProxyStatus
    {
        Untested = 0,    // 未测试
        Valid = 1,       // 有效
        Invalid = 2,    // 无效
        Timeout = 3     // 超时
    }
}
