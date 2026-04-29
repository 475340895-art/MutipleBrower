using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FingerprintBrowser.Models;
using FingerprintBrowser.ViewModels;
using HandyControl.Controls;

namespace FingerprintBrowser.Views
{
    public partial class ProxyWindow
    {
        private readonly ProxyViewModel _viewModel;

        public ProxyWindow()
        {
            InitializeComponent();
            _viewModel = new ProxyViewModel();
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await _viewModel.LoadProxiesAsync();
                ProxyGrid.ItemsSource = _viewModel.Proxies;
                UpdateCountDisplay();
            }
            catch (Exception ex)
            {
                Growl.Error($"加载失败: {ex.Message}");
            }
        }

        private void UpdateCountDisplay()
        {
            var total = _viewModel.Proxies.Count;
            var available = _viewModel.Proxies.Count(p => p.Status == "Available");
            var unavailable = _viewModel.Proxies.Count(p => p.Status == "Unavailable");
            
            ProxyCountText.Text = $"共 {total} 个代理";
            AvailableCountText.Text = $"可用: {available}";
            UnavailableCountText.Text = $"不可用: {unavailable}";
        }

        private async void AddProxy_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddProxyDialog { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                var proxy = dialog.Proxy;
                await _viewModel.AddProxyAsync(proxy);
                ProxyGrid.ItemsSource = null;
                ProxyGrid.ItemsSource = _viewModel.Proxies;
                UpdateCountDisplay();
                Growl.Success("代理添加成功");
            }
        }

        private async void BatchImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                Title = "批量导入代理"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ShowLoading("正在导入...");
                    var count = await _viewModel.BatchImportAsync(dialog.FileName);
                    ProxyGrid.ItemsSource = null;
                    ProxyGrid.ItemsSource = _viewModel.Proxies;
                    UpdateCountDisplay();
                    HideLoading();
                    Growl.Success($"成功导入 {count} 个代理");
                }
                catch (Exception ex)
                {
                    HideLoading();
                    Growl.Error($"导入失败: {ex.Message}");
                }
            }
        }

        private async void BatchTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoading("正在批量测试...");
                await _viewModel.BatchTestAsync();
                ProxyGrid.ItemsSource = null;
                ProxyGrid.ItemsSource = _viewModel.Proxies;
                UpdateCountDisplay();
                HideLoading();
                Growl.Success("批量测试完成");
            }
            catch (Exception ex)
            {
                HideLoading();
                Growl.Error($"测试失败: {ex.Message}");
            }
        }

        private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var result = HandyControl.Controls.MessageBox.Show(
                "确定要删除选中的代理吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _viewModel.DeleteSelectedAsync();
                ProxyGrid.ItemsSource = null;
                ProxyGrid.ItemsSource = _viewModel.Proxies;
                UpdateCountDisplay();
                Growl.Success("删除成功");
            }
        }

        private async void TestProxy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ProxyConfig proxy)
            {
                try
                {
                    btn.IsEnabled = false;
                    proxy.Status = "Testing";
                    var result = await ProxyService.TestProxyAsync(proxy.ToString());
                    proxy.Status = result.Success ? "Available" : "Unavailable";
                    proxy.Latency = result.Latency;
                    UpdateCountDisplay();
                }
                catch (Exception ex)
                {
                    proxy.Status = "Unavailable";
                    Growl.Error($"测试失败: {ex.Message}");
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private async void DeleteProxy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ProxyConfig proxy)
            {
                var result = HandyControl.Controls.MessageBox.Show(
                    "确定要删除此代理吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _viewModel.DeleteProxyAsync(proxy.Id);
                    ProxyGrid.ItemsSource = null;
                    ProxyGrid.ItemsSource = _viewModel.Proxies;
                    UpdateCountDisplay();
                    Growl.Success("删除成功");
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keyword = SearchBox.Text?.ToLower();
            if (string.IsNullOrEmpty(keyword))
            {
                ProxyGrid.ItemsSource = _viewModel.Proxies;
            }
            else
            {
                ProxyGrid.ItemsSource = _viewModel.Proxies
                    .Where(p => p.Host.ToLower().Contains(keyword) ||
                               (p.Remark?.ToLower().Contains(keyword) ?? false))
                    .ToList();
            }
        }

        private void ShowLoading(string message = "加载中...")
        {
            // 简单实现，可根据需要完善
        }

        private void HideLoading()
        {
            // 简单实现
        }
    }

    // 简单的添加代理对话框
    public class AddProxyDialog : Window
    {
        public ProxyConfig Proxy { get; private set; }

        public AddProxyDialog()
        {
            Title = "添加代理";
            Width = 400;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0f0f1a"));

            var grid = new Grid { Margin = new Thickness(24) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var typeCombo = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 12),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e")),
                Foreground = System.Windows.Media.Brushes.White
            };
            typeCombo.Items.Add(new ComboBoxItem { Content = "HTTP" });
            typeCombo.Items.Add(new ComboBoxItem { Content = "HTTPS" });
            typeCombo.Items.Add(new ComboBoxItem { Content = "SOCKS5" });
            typeCombo.SelectedIndex = 0;
            Grid.SetRow(typeCombo, 0);

            var hostBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 12),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e")),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(12, 10, 12, 10)
            };
            Grid.SetRow(hostBox, 1);

            var portBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 12),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e")),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(12, 10, 12, 10)
            };
            Grid.SetRow(portBox, 2);

            var usernameBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 12),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e")),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(12, 10, 12, 10)
            };
            Grid.SetRow(usernameBox, 3);

            var passwordBox = new PasswordBox
            {
                Margin = new Thickness(0, 0, 0, 12),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e")),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(12, 10, 12, 10)
            };
            Grid.SetRow(passwordBox, 4);

            var remarkBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 12),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e")),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(12, 10, 12, 10)
            };
            Grid.SetRow(remarkBox, 5);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(btnPanel, 7);

            var cancelBtn = new Button { Content = "取消", Padding = new Thickness(20, 10, 20, 10), Margin = new Thickness(0, 0, 8, 0) };
            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
            btnPanel.Children.Add(cancelBtn);

            var saveBtn = new Button 
            { 
                Content = "保存", 
                Padding = new Thickness(20, 10, 20, 10),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#667eea"))
            };
            saveBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(hostBox.Text) || string.IsNullOrWhiteSpace(portBox.Text))
                {
                    HandyControl.Controls.MessageBox.Warning("请输入代理地址和端口");
                    return;
                }

                Proxy = new ProxyConfig
                {
                    Type = ((ComboBoxItem)typeCombo.SelectedItem).Content.ToString(),
                    Host = hostBox.Text.Trim(),
                    Port = int.Parse(portBox.Text.Trim()),
                    Username = usernameBox.Text?.Trim(),
                    Password = passwordBox.Password,
                    Remark = remarkBox.Text?.Trim(),
                    Status = "Unknown",
                    CreatedAt = DateTime.Now
                };
                DialogResult = true;
                Close();
            };
            btnPanel.Children.Add(saveBtn);

            grid.Children.Add(typeCombo);
            grid.Children.Add(hostBox);
            grid.Children.Add(portBox);
            grid.Children.Add(usernameBox);
            grid.Children.Add(passwordBox);
            grid.Children.Add(remarkBox);
            grid.Children.Add(btnPanel);

            Content = grid;
        }
    }
}
