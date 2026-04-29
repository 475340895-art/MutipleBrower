using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;
using FingerprintBrowser.ViewModels;

namespace FingerprintBrowser.Views;

public partial class ProxyWindow : HandyControl.Controls.Window
{
    private readonly ProxyViewModel _viewModel;

    public ProxyWindow()
    {
        InitializeComponent();
        _viewModel = new ProxyViewModel();
        DataContext = _viewModel;
        Loaded += ProxyWindow_Loaded;
    }

    private async void ProxyWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadProxiesAsync();
    }

    private async void AddProxy_Click(object sender, RoutedEventArgs e)
    {
        var popup = new AddProxyDialog();
        popup.Owner = this;
        if (popup.ShowDialog() == true)
        {
            await _viewModel.AddProxyAsync(new ProxyConfig
            {
                Host = popup.Host,
                Port = popup.Port,
                Type = popup.ProxyType,
                Username = popup.Username,
                Password = popup.Password
            });
        }
    }

    private async void TestProxy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ProxyConfig proxy)
        {
            await _viewModel.TestSingleProxyAsync(proxy);
        }
    }

    private async void BatchTest_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.BatchTestAsync();
    }

    private async void DeleteProxy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ProxyConfig proxy)
        {
            await _viewModel.DeleteProxyAsync(proxy);
        }
    }

    private async void BatchImport_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.BatchImportAsync();
    }
}

public class AddProxyDialog : HandyControl.Controls.Window
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 80;
    public string ProxyType { get; set; } = "HTTP";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public AddProxyDialog()
    {
        Title = "添加代理";
        Width = 400;
        Height = 350;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(20) };

        panel.Children.Add(new Label { Content = "代理地址:" });
        var hostBox = new System.Windows.Controls.TextBox();
        hostBox.TextChanged += (s, e) => Host = hostBox.Text;
        panel.Children.Add(hostBox);

        panel.Children.Add(new Label { Content = "端口:" });
        var portBox = new System.Windows.Controls.TextBox();
        portBox.TextChanged += (s, e) => int.TryParse(portBox.Text, out var p) && (Port = p) > 0;
        panel.Children.Add(portBox);

        panel.Children.Add(new Label { Content = "类型:" });
        var typeBox = new System.Windows.Controls.ComboBox();
        typeBox.Items.Add("HTTP"); typeBox.Items.Add("HTTPS"); typeBox.Items.Add("SOCKS5");
        typeBox.SelectedIndex = 0;
        typeBox.SelectionChanged += (s, e) => ProxyType = (typeBox.SelectedItem as string) ?? "HTTP";
        panel.Children.Add(typeBox);

        panel.Children.Add(new Label { Content = "用户名 (可选):" });
        var userBox = new System.Windows.Controls.TextBox();
        userBox.TextChanged += (s, e) => Username = userBox.Text;
        panel.Children.Add(userBox);

        panel.Children.Add(new Label { Content = "密码 (可选):" });
        var passBox = new System.Windows.Controls.PasswordBox();
        passBox.PasswordChanged += (s, e) => Password = passBox.Password;
        panel.Children.Add(passBox);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        btnPanel.Children.Add(new System.Windows.Controls.Button { Content = "取消", Margin = new Thickness(0, 0, 10, 0), Width = 80, Click = (s, e) => DialogResult = false });
        btnPanel.Children.Add(new System.Windows.Controls.Button { Content = "确定", Width = 80, IsDefault = true, Click = (s, e) => DialogResult = true });
        panel.Children.Add(btnPanel);

        Content = panel;
    }
}
