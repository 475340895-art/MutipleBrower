using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HandyControl.Themes;

namespace FingerprintBrowser.Views;

public partial class SettingsWindow : HandyControl.Controls.Window
{
    public string SelectedTheme { get; set; } = "深色主题";
    public int StartupDelay { get; set; } = 2;
    public int MaxConcurrency { get; set; } = 5;
    public bool CloseConfirm { get; set; } = true;
    
    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
        DataContext = this;
    }
    
    private void LoadSettings()
    {
        ThemeCombo.SelectedIndex = 0;
        StartupDelayBox.Value = StartupDelay;
        MaxConcurrencyBox.Value = MaxConcurrency;
        CloseConfirmBox.IsChecked = CloseConfirm;
        DataPathBox.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FingerprintBrowser");
        UpdateCacheSize();
    }
    
    private void UpdateCacheSize()
    {
        var path = DataPathBox.Text;
        if (Directory.Exists(path))
        {
            var size = GetDirectorySize(path);
            CacheSizeBox.Text = FormatSize(size);
        }
        else
        {
            CacheSizeBox.Text = "0 B";
        }
    }
    
    private long GetDirectorySize(string path)
    {
        var dir = new DirectoryInfo(path);
        return dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
    }
    
    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
    
    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is ComboBoxItem item)
        {
            SelectedTheme = item.Content?.ToString() ?? "深色主题";
        }
    }
    
    private void BrowseDataPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择数据目录"
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DataPathBox.Text = dialog.SelectedPath;
        }
    }
    
    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        var result = HandyControl.Controls.MessageBox.Show(
            "确定要清理浏览器缓存吗？",
            "确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
            
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var cachePath = Path.Combine(DataPathBox.Text, "Cache");
                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, true);
                }
                HandyControl.Controls.MessageBox.Show("缓存清理完成");
                UpdateCacheSize();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"清理失败：{ex.Message}");
            }
        }
    }
    
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        StartupDelay = (int)StartupDelayBox.Value;
        MaxConcurrency = (int)MaxConcurrencyBox.Value;
        CloseConfirm = CloseConfirmBox.IsChecked ?? true;
        
        // 应用主题
        ApplyTheme(SelectedTheme);
        
        DialogResult = true;
        Close();
    }
    
    private void ApplyTheme(string theme)
    {
        if (theme == "深色主题")
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
        }
        else if (theme == "浅色主题")
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
        }
        else if (theme == "深蓝主题")
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            // 可以添加自定义深蓝主题
        }
    }
    
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
