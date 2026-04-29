using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FingerprintBrowser.Services;
using HandyControl.Themes;
using Serilog;

namespace FingerprintBrowser.ViewModels;

/// <summary>
/// 设置视图模型
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _selectedTheme = "深色";

    [ObservableProperty]
    private string _selectedLanguage = "简体中文";

    [ObservableProperty]
    private bool _autoStartWithSystem;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _showNotifications = true;

    [ObservableProperty]
    private bool _enableLogging = true;

    [ObservableProperty]
    private int _maxConcurrentBrowsers = 10;

    [ObservableProperty]
    private string _browserPath = string.Empty;

    [ObservableProperty]
    private bool _clearCacheOnClose = true;

    [ObservableProperty]
    private bool _clearCookiesOnClose;

    public string[] AvailableThemes { get; } = { "深色", "浅色", "深蓝" };
    public string[] AvailableLanguages { get; } = { "简体中文", "English" };

    public SettingsViewModel()
    {
        _dialogService = ServiceLocator.Get<IDialogService>();
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            var settingsPath = GetSettingsPath();
            if (System.IO.File.Exists(settingsPath))
            {
                var json = System.IO.File.ReadAllText(settingsPath);
                var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json);
                if (settings != null)
                {
                    SelectedTheme = settings.Theme;
                    SelectedLanguage = settings.Language;
                    AutoStartWithSystem = settings.AutoStartWithSystem;
                    MinimizeToTray = settings.MinimizeToTray;
                    ShowNotifications = settings.ShowNotifications;
                    EnableLogging = settings.EnableLogging;
                    MaxConcurrentBrowsers = settings.MaxConcurrentBrowsers;
                    BrowserPath = settings.BrowserPath;
                    ClearCacheOnClose = settings.ClearCacheOnClose;
                    ClearCookiesOnClose = settings.ClearCookiesOnClose;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载设置失败");
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            var settings = new AppSettings
            {
                Theme = SelectedTheme,
                Language = SelectedLanguage,
                AutoStartWithSystem = AutoStartWithSystem,
                MinimizeToTray = MinimizeToTray,
                ShowNotifications = ShowNotifications,
                EnableLogging = EnableLogging,
                MaxConcurrentBrowsers = MaxConcurrentBrowsers,
                BrowserPath = BrowserPath,
                ClearCacheOnClose = ClearCacheOnClose,
                ClearCookiesOnClose = ClearCookiesOnClose
            };

            var settingsPath = GetSettingsPath();
            var directory = System.IO.Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(settingsPath, json);

            Log.Information("设置已保存");
            _dialogService.Success("设置已保存");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存设置失败");
            _dialogService.ShowException(ex);
        }
    }

    partial void OnSelectedThemeChanged(string value)
    {
        try
        {
            var skin = value switch
            {
                "深色" => HandyControl.Themes.SkinType.Dark,
                "浅色" => HandyControl.Themes.SkinType.Light,
                "深蓝" => HandyControl.Themes.SkinType.DarkBlue,
                _ => HandyControl.Themes.SkinType.Dark
            };

            HandyControl.Themes.ThemeManager.Current.ApplicationTheme = skin;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "切换主题失败");
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        if (!_dialogService.Confirm("确定要恢复默认设置吗?")) return;

        SelectedTheme = "深色";
        SelectedLanguage = "简体中文";
        AutoStartWithSystem = false;
        MinimizeToTray = true;
        ShowNotifications = true;
        EnableLogging = true;
        MaxConcurrentBrowsers = 10;
        BrowserPath = string.Empty;
        ClearCacheOnClose = true;
        ClearCookiesOnClose = false;

        SaveSettings();
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        var dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FingerprintBrowser");

        if (Directory.Exists(dataPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", dataPath);
        }
    }

    private static string GetSettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FingerprintBrowser",
            "settings.json");
    }
}

/// <summary>
/// 应用程序设置
/// </summary>
public class AppSettings
{
    public string Theme { get; set; } = "深色";
    public string Language { get; set; } = "简体中文";
    public bool AutoStartWithSystem { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public int MaxConcurrentBrowsers { get; set; } = 10;
    public string BrowserPath { get; set; } = string.Empty;
    public bool ClearCacheOnClose { get; set; } = true;
    public bool ClearCookiesOnClose { get; set; }
}
