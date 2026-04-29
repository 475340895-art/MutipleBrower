namespace FingerprintBrowser;

/// <summary>
/// 应用程序常量
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// 应用程序名称
    /// </summary>
    public const string AppName = "Fingerprint Browser";

    /// <summary>
    /// 应用程序版本
    /// </summary>
    public const string AppVersion = "1.0.0";

    /// <summary>
    /// 数据库文件名
    /// </summary>
    public const string DatabaseFileName = "browser_data.db";

    /// <summary>
    /// 设置文件名
    /// </summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>
    /// 日志目录名
    /// </summary>
    public const string LogDirectoryName = "logs";

    /// <summary>
    /// 默认分组名称
    /// </summary>
    public const string DefaultGroupName = "默认分组";

    /// <summary>
    /// 浏览器启动超时时间（毫秒）
    /// </summary>
    public const int BrowserLaunchTimeout = 30000;

    /// <summary>
    /// 代理测试超时时间（毫秒）
    /// </summary>
    public const int ProxyTestTimeout = 15000;

    /// <summary>
    /// 默认浏览器分辨率
    /// </summary>
    public static readonly (int Width, int Height) DefaultResolution = (1920, 1080);

    /// <summary>
    /// 支持的浏览器渠道
    /// </summary>
    public static readonly string[] SupportedChannels = { "chromium", "chrome", "msedge" };
}

/// <summary>
/// 应用程序信息
/// </summary>
public static class AppInfo
{
    /// <summary>
    /// 获取数据目录路径
    /// </summary>
    public static string GetDataDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.AppName.Replace(" ", ""));
    }

    /// <summary>
    /// 获取数据库文件路径
    /// </summary>
    public static string GetDatabasePath()
    {
        return Path.Combine(GetDataDirectory(), AppConstants.DatabaseFileName);
    }

    /// <summary>
    /// 获取设置文件路径
    /// </summary>
    public static string GetSettingsPath()
    {
        return Path.Combine(GetDataDirectory(), AppConstants.SettingsFileName);
    }

    /// <summary>
    /// 获取日志目录路径
    /// </summary>
    public static string GetLogDirectory()
    {
        return Path.Combine(GetDataDirectory(), AppConstants.LogDirectoryName);
    }

    /// <summary>
    /// 获取缓存目录路径
    /// </summary>
    public static string GetCacheDirectory()
    {
        return Path.Combine(GetDataDirectory(), "cache");
    }

    /// <summary>
    /// 确保必要的目录存在
    /// </summary>
    public static void EnsureDirectoriesExist()
    {
        var directories = new[]
        {
            GetDataDirectory(),
            GetLogDirectory(),
            GetCacheDirectory()
        };

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
