using System;
using System.IO;
using System.Text.Json;

namespace FingerprintBrowser.Services
{
    public class AppConfig
    {
        public string Theme { get; set; } = "Dark";
        public int LaunchDelay { get; set; } = 0;
        public int ConcurrentBrowserCount { get; set; } = 5;
        public int ProxyTimeout { get; set; } = 10;
        public bool CloseWithX { get; set; } = false;
        public string DataPath { get; set; } = AppConstants.DefaultDataPath;
        public string BackupFrequency { get; set; } = "None";
    }

    public static class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            AppConstants.DefaultDataPath, "settings.json");

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch
            {
                // 忽略错误，返回默认配置
            }
            return new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存设置失败: {ex.Message}", ex);
            }
        }
    }
}
