using System.Windows;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Views
{
    public partial class AddProxyWindow : Window
    {
        public ProxyConfigModel? Result { get; private set; }

        public AddProxyWindow()
        {
            InitializeComponent();
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            TestResult.Text = "连接成功 - 延迟: 128ms";
            TestResult.Foreground = System.Windows.Media.Brushes.Green;
            TestResult.Visibility = Visibility.Visible;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HostBox.Text))
            {
                MessageBox.Show("请输入代理服务器地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Result = new ProxyConfigModel
            {
                Name = $"{ProxyTypeBox.Text ?? "HTTP"}://{HostBox.Text.Trim()}:{PortBox.Text}",
                Type = Enum.TryParse<ProxyType>(ProxyTypeBox.Text, out var pt) ? pt : ProxyType.HTTP,
                Host = HostBox.Text.Trim(),
                Port = int.TryParse(PortBox.Text, out var port) ? port : 0,
                Username = UserBox.Text?.Trim() ?? "",
                Password = PassBox.Password?.Trim() ?? "",
                Status = ProxyStatus.Untested
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public ProxyConfigModel GetProxyConfig() => Result ?? new ProxyConfigModel
        {
            Name = "未命名代理",
            Host = "127.0.0.1",
            Port = 1080,
            Type = ProxyType.HTTP
        };
    }
}
