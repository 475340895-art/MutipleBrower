using System.Windows;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Views
{
    public partial class EnvironmentEditWindow : Window
    {
        private BrowserEnvironment? _editTarget;

        public EnvironmentEditWindow()
        {
            InitializeComponent();
            Title = "新建环境";
            WebRTCCheck.IsChecked = true;
            JSCheck.IsChecked = true;
            CookieCheck.IsChecked = true;
            ResolutionBox.Text = "1920x1080";
            UABox.Text = "Chrome 120 / Win10";
        }

        public void SetEnvironment(BrowserEnvironment env)
        {
            _editTarget = env;
            Title = "编辑环境";
            NameBox.Text = env.Name;
            RemarksBox.Text = env.Remarks ?? "";
            ProxyHostBox.Text = env.ProxyHost ?? "";
            ProxyPortBox.Text = env.ProxyPort > 0 ? env.ProxyPort.ToString() : "";
            ProxyUserBox.Text = env.ProxyUsername ?? "";
            WebRTCCheck.IsChecked = env.EnableWebRTC;
            JSCheck.IsChecked = env.EnableJavaScript;
            CookieCheck.IsChecked = env.EnableCookies;
            ResolutionBox.Text = env.Resolution;
            UABox.Text = env.UserAgent ?? "";
        }

        public BrowserEnvironment GetEnvironment()
        {
            var env = _editTarget ?? new BrowserEnvironment();
            env.Name = NameBox.Text?.Trim() ?? "未命名";
            env.Remarks = RemarksBox.Text?.Trim();
            env.ProxyHost = string.IsNullOrWhiteSpace(ProxyHostBox.Text) ? null : ProxyHostBox.Text.Trim();
            env.ProxyPort = int.TryParse(ProxyPortBox.Text, out var port) ? port : 0;
            env.ProxyUsername = string.IsNullOrWhiteSpace(ProxyUserBox.Text) ? null : ProxyUserBox.Text.Trim();
            env.EnableWebRTC = WebRTCCheck.IsChecked == true;
            env.EnableJavaScript = JSCheck.IsChecked == true;
            env.EnableCookies = CookieCheck.IsChecked == true;
            env.Resolution = string.IsNullOrWhiteSpace(ResolutionBox.Text) ? "1920x1080" : ResolutionBox.Text.Trim();
            env.UserAgent = string.IsNullOrWhiteSpace(UABox.Text) ? null : UABox.Text.Trim();
            return env;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("请输入环境名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
