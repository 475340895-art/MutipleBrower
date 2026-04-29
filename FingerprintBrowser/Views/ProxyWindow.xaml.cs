using System.Windows;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Views;

public partial class ProxyWindow : HandyControl.Controls.Window
{
    public ProxyWindow()
    {
        InitializeComponent();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var host = HostTextBox?.Text?.Trim() ?? "";
        var portStr = PortTextBox?.Text?.Trim() ?? "";
        var name = NameTextBox?.Text?.Trim() ?? "";
        
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portStr))
        {
            HandyControl.Controls.MessageBox.Show("请输入代理地址和端口", "提示");
            return;
        }

        if (!int.TryParse(portStr, out var port))
        {
            HandyControl.Controls.MessageBox.Show("端口必须是数字", "提示");
            return;
        }

        var proxyType = ProxyTypeComboBox?.SelectedIndex ?? 0;
        // Proxy will be added via database
        HandyControl.Controls.MessageBox.Show("代理添加功能需要数据库支持", "提示");
    }

    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        HandyControl.Controls.MessageBox.Show("代理测试功能", "提示");
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        HandyControl.Controls.MessageBox.Show("批量导入功能", "提示");
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
