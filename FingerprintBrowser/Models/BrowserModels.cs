using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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

    public int GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public EnvironmentGroup? Group { get; set; }

    // 状态
    public int Status { get; set; } = 0;
    
    [NotMapped]
    public string StatusText => Status switch
    {
        1 => "Running",
        2 => "Starting",
        3 => "Stopping",
        4 => "Error",
        _ => "Idle"
    };
    
    [NotMapped]
    public string StatusDisplay => Status switch
    {
        1 => "运行中",
        2 => "启动中",
        3 => "停止中",
        4 => "错误",
        _ => "空闲"
    };

    public DateTime? LastRunTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 基本配置
    [MaxLength(500)]
    public string? Remark { get; set; }

    // 浏览器指纹配置
    public string? UserAgent { get; set; }
    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;
    public string? Timezone { get; set; } = "Asia/Shanghai";
    public string? TimezoneId { get; set; }
    public string? Languages { get; set; } = "zh-CN,zh;q=0.9,en;q=0.8";
    public string? Platform { get; set; } = "Win32";
    public string? HardwareConcurrency { get; set; } = "8";
    public string? DeviceMemory { get; set; } = "8";
    public string? WebGlVendor { get; set; }
    public string? WebGlRenderer { get; set; }
    public string? Canvas { get; set; } = "random";
    public string? AudioContext { get; set; } = "random";
    public string? WebGLInfo { get; set; } = "random";
    public string? MediaDevices { get; set; } = "random";
    public string? BrowserType { get; set; } = "Chromium";

    // 代理配置
    public bool UseProxy { get; set; } = false;
    public int? ProxyId { get; set; }
    [ForeignKey(nameof(ProxyId))]
    public ProxyConfig? Proxy { get; set; }
    [MaxLength(500)]
    public string? ProxyConfig { get; set; }

    // 启动URL
    [MaxLength(1000)]
    public string? StartupUrl { get; set; }

    // Cookie导入
    public string? Cookies { get; set; }

    // 备注（兼容旧字段）
    [MaxLength(500)]
    public string? Remarks
    {
        get => Remark;
        set => Remark = value;
    }

    // 是否收藏
    public bool IsFavorite { get; set; } = false;

    // 运行时信息（不存入数据库）
    [NotMapped]
    public string? RunningUrl { get; set; }

    [NotMapped]
    public int? ProcessId { get; set; }
    
    // UI显示用
    [NotMapped]
    public string? GroupName { get; set; }
    
    [NotMapped]
    public string? ProxyInfo { get; set; }

    public BrowserEnvironment Clone()
    {
        return new BrowserEnvironment
        {
            Name = Name + " (副本)",
            Remark = Remark,
            GroupId = GroupId,
            UserAgent = UserAgent,
            ScreenWidth = ScreenWidth,
            ScreenHeight = ScreenHeight,
            Timezone = Timezone,
            TimezoneId = TimezoneId,
            Languages = Languages,
            Platform = Platform,
            HardwareConcurrency = HardwareConcurrency,
            DeviceMemory = DeviceMemory,
            WebGlVendor = WebGlVendor,
            WebGlRenderer = WebGlRenderer,
            Canvas = Canvas,
            AudioContext = AudioContext,
            WebGLInfo = WebGLInfo,
            MediaDevices = MediaDevices,
            BrowserType = BrowserType,
            UseProxy = UseProxy,
            ProxyId = ProxyId,
            ProxyConfig = ProxyConfig,
            StartupUrl = StartupUrl,
            Cookies = Cookies,
            CreatedAt = DateTime.Now
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
    public string Color { get; set; } = "#3B82F6";

    public int SortOrder { get; set; } = 0;
    public bool IsExpanded { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<BrowserEnvironment> Environments { get; set; } = new List<BrowserEnvironment>();

    [NotMapped]
    public int RunningCount => Environments?.Count(e => e.Status == 1) ?? 0;

    [NotMapped]
    public int EnvironmentCount => Environments?.Count ?? 0;
}

/// <summary>
/// 代理配置
/// </summary>
public class ProxyConfig
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Type { get; set; } = "HTTP";

    [Required]
    [MaxLength(200)]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 80;

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(100)]
    public string? Password { get; set; }

    [MaxLength(200)]
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastTestTime { get; set; }

    // 状态（不存入数据库）
    [NotMapped]
    public string Status { get; set; } = "Unknown";

    [NotMapped]
    public int Latency { get; set; } = 0;

    [NotMapped]
    public bool IsSelected { get; set; } = false;

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
        {
            return $"{Type.ToLower()}://{Username}:{Password}@{Host}:{Port}";
        }
        return $"{Type.ToLower()}://{Host}:{Port}";
    }
}
