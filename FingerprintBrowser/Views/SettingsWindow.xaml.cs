using System;
using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using Window = HandyControl.Controls.Window;

namespace FingerprintBrowser.Views
{
    public partial class SettingsWindow : Window
    {
        public int MaxConcurrency { get; set; } = 5;
        public int StartupDelay { get; set; } = 1000;
        public string CurrentTheme { get; set; } = "Dark";

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            MaxConcurrency = Properties.Settings.Default.MaxConcurrency;
            StartupDelay = Properties.Settings.Default.StartupDelay;
            CurrentTheme = Properties.Settings.Default.Theme;

            MaxConcurrencySlider.Value = MaxConcurrency;
            StartupDelaySlider.Value = StartupDelay;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.MaxConcurrency = MaxConcurrency;
            Properties.Settings.Default.StartupDelay = StartupDelay;
            Properties.Settings.Default.Theme = CurrentTheme;
            Properties.Settings.Default.Save();

            Growl.Success("设置已保存");
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MaxConcurrencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MaxConcurrencyText != null)
            {
                MaxConcurrency = (int)e.NewValue;
                MaxConcurrencyText.Text = $"并发数: {MaxConcurrency}";
            }
        }

        private void StartupDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (StartupDelayText != null)
            {
                StartupDelay = (int)e.NewValue;
                StartupDelayText.Text = $"启动延迟: {StartupDelay}ms";
            }
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Header != null)
            {
                CurrentTheme = menuItem.Header.ToString()?.Replace("主题: ", "").Trim() ?? "Dark";
            }
        }
    }
}
