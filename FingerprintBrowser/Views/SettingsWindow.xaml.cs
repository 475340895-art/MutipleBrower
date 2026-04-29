using System.Windows;

namespace FingerprintBrowser.Views
{
    public partial class SettingsWindow : HandyControl.Controls.Window
    {
        public int MaxConcurrency { get; set; } = 5;
        public int StartupDelay { get; set; } = 2;
        public bool AutoUpdate { get; set; } = true;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            MaxConcurrencyNumericUpDown.Value = MaxConcurrency;
            StartupDelayNumericUpDown.Value = StartupDelay;
            AutoUpdateCheckBox.IsChecked = AutoUpdate;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MaxConcurrency = (int)(MaxConcurrencyNumericUpDown.Value ?? 5);
            StartupDelay = (int)(StartupDelayNumericUpDown.Value ?? 2);
            AutoUpdate = AutoUpdateCheckBox.IsChecked ?? true;

            HandyControl.Controls.MessageBox.Show("设置已保存", "提示");
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            MaxConcurrency = 5;
            StartupDelay = 2;
            AutoUpdate = true;
            LoadSettings();
        }
    }
}
