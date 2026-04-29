using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FingerprintBrowser.ViewModels;

/// <summary>
/// 环境编辑视图模型
/// </summary>
public partial class EnvironmentEditViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private BrowserEnvironment _environment = new();

    [ObservableProperty]
    private ObservableCollection<EnvironmentGroup> _groups = new();

    [ObservableProperty]
    private ObservableCollection<ProxyConfig> _proxies = new();

    [ObservableProperty]
    private bool _isEditing;

    public EnvironmentEditViewModel()
    {
        _dialogService = ServiceLocator.Get<IDialogService>();
        _ = LoadComboDataAsync();
    }

    private async Task LoadComboDataAsync()
    {
        try
        {
            using var context = new BrowserDbContext();
            var groups = await context.Groups.OrderBy(g => g.SortOrder).ToListAsync();
            var proxies = await context.Proxies.OrderBy(p => p.Name).ToListAsync();

            Groups = new ObservableCollection<EnvironmentGroup>(groups);
            Proxies = new ObservableCollection<ProxyConfig>(proxies);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载下拉数据失败");
        }
    }

    public void LoadEnvironment(BrowserEnvironment environment)
    {
        Environment = environment;
        IsEditing = environment.Id > 0;
    }

    public void NewEnvironment(int groupId = 1)
    {
        Environment = new BrowserEnvironment
        {
            GroupId = groupId,
            ScreenWidth = 1920,
            ScreenHeight = 1080,
            TimeZone = 8,
            Language = "zh-CN"
        };
        IsEditing = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Environment.Name))
        {
            _dialogService.Warning("请输入环境名称");
            return;
        }

        try
        {
            using var context = new BrowserDbContext();

            if (IsEditing)
            {
                context.Environments.Update(Environment);
            }
            else
            {
                Environment.CreatedAt = DateTime.Now;
                context.Environments.Add(Environment);
            }

            Environment.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();

            Log.Information("保存环境成功: {Name}", Environment.Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存环境失败");
            _dialogService.ShowException(ex);
        }
    }

    /// <summary>
    /// 生成随机指纹
    /// </summary>
    [RelayCommand]
    private void GenerateRandomFingerprint()
    {
        var random = new Random();

        // 随机屏幕分辨率
        var resolutions = new[]
        {
            (1920, 1080), (1366, 768), (1440, 900), (1600, 900),
            (1280, 720), (1536, 864), (2560, 1440), (3840, 2160)
        };
        var (w, h) = resolutions[random.Next(resolutions.Length)];
        Environment.ScreenWidth = w;
        Environment.ScreenHeight = h;

        // 随机时区
        var timezones = new[] { -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        Environment.TimeZone = timezones[random.Next(timezones.Length)];

        // 随机语言
        var languages = new[] { "zh-CN", "en-US", "en-GB", "ja-JP", "ko-KR", "de-DE", "fr-FR", "es-ES" };
        Environment.Language = languages[random.Next(languages.Length)];

        // 随机平台
        var platforms = new[] { "Win32", "MacIntel", "Linux x86_64" };
        Environment.Platform = platforms[random.Next(platforms.Length)];

        // 随机硬件信息
        var cores = new[] { "4", "6", "8", "12", "16" };
        Environment.HardwareConcurrency = cores[random.Next(cores.Length)];

        var memory = new[] { "4", "8", "16", "32" };
        Environment.DeviceMemory = memory[random.Next(memory.Length)];

        // 随机 WebGL 信息
        var vendors = new[] { "Intel Inc.", "NVIDIA Corporation", "AMD" };
        var renderers = new[]
        {
            "Intel Iris OpenGL Engine",
            "NVIDIA GeForce GTX 1060 OpenGL Engine",
            "AMD Radeon R9 380 Series OpenGL Engine"
        };
        Environment.WebGlVendor = vendors[random.Next(vendors.Length)];
        Environment.WebGlRenderer = renderers[random.Next(renderers.Length)];

        // 随机 User Agent
        var chromeVersions = new[]
        {
            "119.0.6045.106",
            "120.0.6099.109",
            "121.0.6167.85",
            "122.0.6261.57"
        };
        var version = chromeVersions[random.Next(chromeVersions.Length)];
        Environment.UserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{version} Safari/537.36";

        OnPropertyChanged(nameof(Environment));
    }

    /// <summary>
    /// 重置指纹
    /// </summary>
    [RelayCommand]
    private void ResetFingerprint()
    {
        Environment.ScreenWidth = 1920;
        Environment.ScreenHeight = 1080;
        Environment.TimeZone = 8;
        Environment.Language = "zh-CN";
        Environment.Platform = "Win32";
        Environment.HardwareConcurrency = "8";
        Environment.DeviceMemory = "8";
        Environment.WebGlVendor = null;
        Environment.WebGlRenderer = null;
        Environment.UserAgent = null;

        OnPropertyChanged(nameof(Environment));
    }
}
