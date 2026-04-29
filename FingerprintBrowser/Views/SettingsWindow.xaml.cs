using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FingerprintBrowser.Services;
using HandyControl.Controls;

namespace FingerprintBrowser.Views
{
    public partial class SettingsWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 加载当前设置
            DataPathTextBox.Text = AppConstants.DefaultDataPath;
            
            // 从配置加载（如果存在）
            var config = AppSettings.Load();
            LaunchDelayTextBox.Text = config.LaunchDelay.ToString();
            ConcurrentCountTextBox.Text = config.ConcurrentBrowserCount.ToString();
            ProxyTimeoutTextBox.Text = config.ProxyTimeout.ToString();
            CloseWithXCheckBox.IsChecked = config.CloseWithX;
            
            // 主题
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag?.ToString() == config.Theme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 主题切换可以实时预览
        }

        private void BrowseDataPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择数据存储路径",
                SelectedPath = DataPathTextBox.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DataPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var result = HandyControl.Controls.MessageBox.Show(
                "确定要恢复所有设置为默认值吗？",
                "确认重置",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ThemeComboBox.SelectedIndex = 0;
                LaunchDelayTextBox.Text = "0";
                ConcurrentCountTextBox.Text = "5";
                ProxyTimeoutTextBox.Text = "10";
                CloseWithXCheckBox.IsChecked = false;
                DataPathTextBox.Text = AppConstants.DefaultDataPath;
                Growl.Success("已恢复默认设置");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (!int.TryParse(LaunchDelayTextBox.Text, out int launchDelay) || launchDelay < 0)
                {
                    Growl.Warning("启动延迟必须是大于等于0的数字");
                    return;
                }

                if (!int.TryParse(ConcurrentCountTextBox.Text, out int concurrentCount) || concurrentCount < 1 || concurrentCount > 50)
                {
                    Growl.Warning("并发数量必须在1-50之间");
                    return;
                }

                if (!int.TryParse(ProxyTimeoutTextBox.Text, out int proxyTimeout) || proxyTimeout < 1)
                {
                    Growl.Warning("代理超时必须是大于0的数字");
                    return;
                }

                // 保存配置
                var config = new AppConfig
                {
                    Theme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Dark",
                    LaunchDelay = launchDelay,
                    ConcurrentBrowserCount = concurrentCount,
                    ProxyTimeout = proxyTimeout,
                    CloseWithX = CloseWithXCheckBox.IsChecked ?? false,
                    DataPath = DataPathTextBox.Text
                };

                AppSettings.Save(config);

                // 应用主题
                ApplyTheme(config.Theme);

                Growl.Success("设置已保存");
                Close();
            }
            catch (Exception ex)
            {
                Growl.Error($"保存失败: {ex.Message}");
            }
        }

        private void ApplyTheme(string theme)
        {
            var app = Application.Current;
            var resources = app.Resources.MergedDictionaries;

            // 移除现有主题
            var toRemove = resources.Where(d => 
                d.Source?.ToString().Contains("HandyControl") == true &&
                d.Source.ToString().Contains("Skin")).ToList();
            
            foreach (var dict in toRemove)
            {
                resources.Remove(dict);
            }

            // 添加新主题
            string skinPath = theme switch
            {
                "Light" => "pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml",
                "DeepBlue" => "pack://application:,,,/HandyControl;component/Themes/SkinDeepBlue.xaml",
                _ => "pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml"
            };

            resources.Insert(0, new ResourceDictionary { Source = new Uri(skinPath) });
        }
    }
}
