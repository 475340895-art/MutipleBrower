using System.Windows;
using FingerprintBrowser.ViewModels;

namespace FingerprintBrowser.Views
{
    public partial class ProxyWindow : HandyControl.Controls.Window
    {
        private readonly ProxyViewModel _viewModel;

        public ProxyWindow()
        {
            InitializeComponent();
            _viewModel = new ProxyViewModel();
            DataContext = _viewModel;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadProxiesAsync();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox?.Text ?? string.Empty;
            var host = HostTextBox?.Text ?? string.Empty;
            var portText = PortTextBox?.Text ?? "0";

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(host))
            {
                HandyControl.Controls.MessageBox.Show("请填写名称和主机地址", "提示");
                return;
            }

            if (!int.TryParse(portText, out var port))
            {
                HandyControl.Controls.MessageBox.Show("端口必须是数字", "提示");
                return;
            }

            var proxy = new Models.ProxyConfig
            {
                Name = name,
                Host = host,
                Port = port,
                Type = TypeComboBox?.SelectedItem?.ToString() ?? "HTTP"
            };

            await _viewModel.AddProxyAsync(proxy);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedProxy == null)
            {
                HandyControl.Controls.MessageBox.Show("请选择要删除的代理", "提示");
                return;
            }

            var result = HandyControl.Controls.MessageBox.Show(
                $"确定删除代理 \"{_viewModel.SelectedProxy.Name}\" 吗?",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.DeleteProxyAsync(_viewModel.SelectedProxy.Id);
            }
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedProxy == null)
            {
                HandyControl.Controls.MessageBox.Show("请选择要测试的代理", "提示");
                return;
            }

            await _viewModel.TestSingleProxyAsync(_viewModel.SelectedProxy);
        }

        private async void BatchTestButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.BatchTestAsync();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "文本文件|*.txt|所有文件|*.*",
                Title = "导入代理"
            };

            if (dialog.ShowDialog() == true)
            {
                var text = System.IO.File.ReadAllText(dialog.FileName);
                _ = _viewModel.BatchImportAsync(text);
            }
        }
    }
}
