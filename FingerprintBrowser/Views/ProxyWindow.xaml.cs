using System.Windows;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Views
{
    public partial class ProxyWindow : HandyControl.Controls.Window
    {
        public ProxyWindow()
        {
            InitializeComponent();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddProxyWindow { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                // Add to grid
                var list = ProxyGrid.ItemsSource as System.Collections.Generic.List<ProxyConfig>;
                if (list != null)
                {
                    list.Add(dialog.Result);
                    ProxyGrid.Items.Refresh();
                }
            }
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("代理测试功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ProxyGrid.SelectedItem is ProxyConfig proxy)
            {
                var result = MessageBox.Show($"确认删除代理 {proxy.Host}?", "确认", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var list = ProxyGrid.ItemsSource as System.Collections.Generic.List<ProxyConfig>;
                    if (list != null)
                    {
                        list.Remove(proxy);
                        ProxyGrid.Items.Refresh();
                    }
                }
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("批量导入功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
