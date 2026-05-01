using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace FingerprintBrowser.Views
{
    public partial class SettingsWindow : HandyControl.Controls.Window
    {
        public string SelectedTheme { get; set; } = "浅色主题";
        public int StartupDelay { get; set; } = 2;
        public int MaxConcurrency { get; set; } = 5;
        public bool CloseConfirm { get; set; } = true;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
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
                var dir = new DirectoryInfo(path);
                var size = dir.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                CacheSizeBox.Text = FormatSize(size);
            }
            else
            {
                CacheSizeBox.Text = "0 B";
            }
        }

        private static string FormatSize(long bytes)
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
                SelectedTheme = item.Content?.ToString() ?? "浅色主题";
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
            var result = MessageBox.Show(
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
                    MessageBox.Show("缓存清理完成");
                    UpdateCacheSize();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"清理失败：{ex.Message}");
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            StartupDelay = (int)StartupDelayBox.Value;
            MaxConcurrency = (int)MaxConcurrencyBox.Value;
            CloseConfirm = CloseConfirmBox.IsChecked ?? true;

            // Apply theme via ResourceDictionary
            ApplyTheme(SelectedTheme);

            DialogResult = true;
            Close();
        }

        private void ApplyTheme(string theme)
        {
            var app = Application.Current;
            if (theme == "深色主题")
            {
                app.Resources.MergedDictionaries.Clear();
                app.Resources.MergedDictionaries.Add(new HandyControl.Themes.DarkResourceDictionary());
            }
            else
            {
                app.Resources.MergedDictionaries.Clear();
                app.Resources.MergedDictionaries.Add(new HandyControl.Themes.DefaultResourceDictionary());
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
