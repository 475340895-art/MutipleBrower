using System;
using System.Windows;
using System.Windows.Controls;
using FingerprintBrowser.Models;
using FingerprintBrowser.ViewModels;
using HandyControl.Controls;
using Window = HandyControl.Controls.Window;

namespace FingerprintBrowser.Views
{
    public partial class ProxyWindow : Window
    {
        private readonly ProxyViewModel _viewModel;

        public ProxyWindow()
        {
            InitializeComponent();
            _viewModel = new ProxyViewModel();
            DataContext = _viewModel;
            Loaded += async (s, e) => await _viewModel.LoadProxiesAsync();
        }

        private async void AddProxy_Click(object sender, RoutedEventArgs e)
        {
            var input = new TextInputWindow("添加代理", "请输入代理名称：");
            if (input.ShowDialog() == true)
            {
                await _viewModel.AddProxyAsync(input.InputText);
            }
        }

        private async void DeleteProxy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ProxyConfig proxy)
            {
                await _viewModel.DeleteProxyAsync(proxy);
            }
        }

        private async void TestProxy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ProxyConfig proxy)
            {
                await _viewModel.TestSingleProxyAsync(proxy);
            }
        }

        private async void BatchTest_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.BatchTestAsync();
        }

        private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.DeleteSelectedAsync();
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
                var content = await System.IO.File.ReadAllTextAsync(dialog.FileName);
                await _viewModel.BatchImportAsync(content);
            }
        }

        private async void SaveProxy_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadProxiesAsync();
            Growl.Success("保存成功");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
