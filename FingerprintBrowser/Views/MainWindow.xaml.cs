using System.Windows;
using FingerprintBrowser.ViewModels;
using FingerprintBrowser.Views;
using HandyControl.Controls;

namespace FingerprintBrowser.Views
{
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }

        private async void NewEnvironment_Click(object sender, RoutedEventArgs e)
        {
            var window = new EnvironmentEditWindow();
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                await _viewModel.LoadEnvironmentsAsync();
            }
        }

        private void EditEnvironment_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedEnvironment == null)
            {
                MessageBox.Show("请选择要编辑的环境", "提示");
                return;
            }

            var window = new EnvironmentEditWindow(_viewModel.SelectedEnvironment.Id);
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                _ = _viewModel.LoadEnvironmentsAsync();
            }
        }

        private async void DeleteEnvironment_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedEnvironment == null)
            {
                MessageBox.Show("请选择要删除的环境", "提示");
                return;
            }

            var result = MessageBox.Show(
                $"确定删除环境 \"{_viewModel.SelectedEnvironment.Name}\" 吗?",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.DeleteEnvironmentAsync(_viewModel.SelectedEnvironment.Id);
            }
        }

        private async void CopyEnvironment_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedEnvironment == null)
            {
                MessageBox.Show("请选择要复制的环境", "提示");
                return;
            }

            await _viewModel.CopyEnvironmentAsync(_viewModel.SelectedEnvironment.Id);
        }

        private async void StartBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedEnvironment == null)
            {
                MessageBox.Show("请选择要启动的环境", "提示");
                return;
            }

            await _viewModel.StartBrowserAsync(_viewModel.SelectedEnvironment);
        }

        private async void StopBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedEnvironment == null)
            {
                MessageBox.Show("请选择要停止的环境", "提示");
                return;
            }

            await _viewModel.StopBrowserAsync(_viewModel.SelectedEnvironment);
        }

        private async void BatchStart_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.BatchStartAsync();
        }

        private async void BatchStop_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.BatchStopAsync();
        }

        private void OpenProxyWindow_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProxyWindow { Owner = this };
            window.ShowDialog();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new SettingsWindow { Owner = this };
            window.ShowDialog();
        }

        private void ChangeTheme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Header != null)
            {
                var themeName = menuItem.Header.ToString()?.Replace("深色", "Dark")
                    .Replace("浅色", "Light")
                    .Replace("深蓝", "Purple") ?? "Dark";
                _viewModel.ChangeTheme(themeName);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadEnvironmentsAsync();
        }
    }
}
