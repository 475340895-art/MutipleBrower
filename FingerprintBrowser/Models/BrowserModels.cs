using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FingerprintBrowser.Models;

/// <summary>
/// 浏览器环境模型
/// </summary>
public class BrowserEnvironment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = "新环境";

    public int GroupId { get; set; }

    [ForeignKey("GroupId")]
    public virtual EnvironmentGroup? Group { get; set; }

    // UI显示用字段（非数据库字段）
    [NotMapped]
    public string GroupName { get; set; } = "默认分组";

    [NotMapped]
    public string ProxyInfo { get; set; } = "无代理";

    [MaxLength(500)]
    public string? Remark { get; set; }

    [MaxLength(500)]
    public string? StartupUrl { get; set; } = "https://www.google.com";

    // 指纹配置
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;

    [MaxLength(100)]
    public string? Timezone { get; set; } = "Asia/Shanghai";

    [MaxLength(200)]
    public string? Languages { get; set; } = "zh-CN,zh,en-US,en";

    [MaxLength(50)]
    public string? WebglVendor { get; set; }

    [MaxLength(50)]
    public string? WebglRenderer { get; set; }

    [MaxLength(100)]
    public string? BrowserType { get; set; } = "chromium";

    // 代理配置
    public bool UseProxy { get; set; }
    public int? ProxyId { get; set; }

    [ForeignKey("ProxyId")]
    public virtual ProxyConfig? Proxy { get; set; }

    [MaxLength(500)]
    public string? ProxyConfig { get; set; }

    // 状态: 0=空闲, 1=运行中, 2=启动中, 3=停止中, 4=错误
    public int Status { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? LastRunTime { get; set; }

    /// <summary>
    /// 克隆环境
    /// </summary>
    public BrowserEnvironment Clone()
    {
        return new BrowserEnvironment
        {
            Name = this.Name + " (副本)",
            GroupId = this.GroupId,
            Remark = this.Remark,
            StartupUrl = this.StartupUrl,
            UserAgent = this.UserAgent,
            ScreenWidth = this.ScreenWidth,
            ScreenHeight = this.ScreenHeight,
            Timezone = this.Timezone,
            Languages = this.Languages,
            WebglVendor = this.WebglVendor,
            WebglRenderer = this.WebglRenderer,
            BrowserType = this.BrowserType,
            UseProxy = this.UseProxy,
            ProxyId = this.ProxyId,
            ProxyConfig = this.ProxyConfig,
            Status = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}

/// <summary>
/// 环境分组模型
/// </summary>
public class EnvironmentGroup
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "默认分组";

    [MaxLength(20)]
    public string Color { get; set; } = "#667eea";

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<BrowserEnvironment> Environments { get; set; } = new List<BrowserEnvironment>();
}

/// <summary>
/// 代理配置模型
/// </summary>
public class ProxyConfig
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "新代理";

    [Required]
    [MaxLength(500)]
    public string ProxyAddress { get; set; } = "";

    [MaxLength(50)]
    public string ProxyType { get; set; } = "http"; // http, https, socks5

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(100)]
    public string? Password { get; set; }

    public int? Port { get; set; }

    // 状态: 0=未测试, 1=可用, 2=不可用
    public int Status { get; set; } = 0;

    public DateTime? LastTestTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<BrowserEnvironment> Environments { get; set; } = new List<BrowserEnvironment>();
}
